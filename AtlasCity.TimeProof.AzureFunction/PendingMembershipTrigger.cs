﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AtlasCity.TimeProof.Abstractions.DAO;
using AtlasCity.TimeProof.Abstractions.Repository;
using AtlasCity.TimeProof.Abstractions.Services;
using AtlasCity.TimeProof.Common.Lib.Extensions;
using Dawn;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace AtlasCity.TimeProof.AzureFunction
{
    public class PendingMembershipTrigger
    {
        private readonly ILogger _logger;
        private readonly IPendingMembershipChangeRepository _pendingMembershipChangeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPricePlanRepository _pricePlanRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentService _paymentService;

        public PendingMembershipTrigger(
            ILogger logger,
            IPendingMembershipChangeRepository pendingMembershipChangeRepository,
            IUserRepository userRepository,
            IPricePlanRepository pricePlanRepository,
            IPaymentRepository paymentRepository,
            IPaymentService paymentService
            )
        {
            Guard.Argument(logger, nameof(logger)).NotNull();
            Guard.Argument(pendingMembershipChangeRepository, nameof(pendingMembershipChangeRepository)).NotNull();
            Guard.Argument(userRepository, nameof(userRepository)).NotNull();
            Guard.Argument(pricePlanRepository, nameof(pricePlanRepository)).NotNull();
            Guard.Argument(paymentRepository, nameof(paymentRepository)).NotNull();
            Guard.Argument(paymentService, nameof(paymentService)).NotNull();

            _logger = logger;
            _pendingMembershipChangeRepository = pendingMembershipChangeRepository;
            _userRepository = userRepository;
            _pricePlanRepository = pricePlanRepository;
            _paymentRepository = paymentRepository;
            _paymentService = paymentService;
        }

        [FunctionName("PendingMembershipFunction")]
        public async Task Run([TimerTrigger("0 00 6 * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProcessPendingMemberships();
            await ProcessRenewingMemberships();
        }

        private async Task ProcessPendingMemberships()
        {
            var cancellationToken = new CancellationToken();
            var currentDate = DateTime.UtcNow;
            var pendingMembershipChanges = await _pendingMembershipChangeRepository.GetByDate(currentDate, cancellationToken);
            if (!pendingMembershipChanges.Any())
            {
                _logger.Information($"Unable to find any message at '{currentDate}'.");
                return;
            }

            _logger.Information($"Retrieved '{pendingMembershipChanges.Count()}' pending membership changes at '{currentDate}'.");

            foreach (var pendingChanges in pendingMembershipChanges)
            {
                try
                {
                    await ChangePricePlan(pendingChanges.UserId, pendingChanges.NewPricePlanId, cancellationToken);
                    await _pendingMembershipChangeRepository.DeleteByUser(pendingChanges.UserId, cancellationToken);
                    _logger.Information($"Removed all pending plan update for user '{pendingChanges.UserId}'.");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    pendingChanges.Error = ex.Message;
                    try
                    {
                        await _pendingMembershipChangeRepository.Update(pendingChanges, cancellationToken);
                    }
                    catch (Exception exRepo) { _logger.Error(exRepo.Message); }
                }
            }
        }

        private async Task ProcessRenewingMemberships()
        {
            var cancellationToken = new CancellationToken();
            var currentDate = DateTime.UtcNow;
            var pendingMembershipsToRenew = await _userRepository.GetRenewalMembershipByDate(currentDate, cancellationToken);
            if (!pendingMembershipsToRenew.Any())
            {
                _logger.Information($"There is no pending renew membership on  '{currentDate}'.");
                return;
            }

            _logger.Information($"Found '{pendingMembershipsToRenew.Count()}' pending membership to renew at '{currentDate}'.");

            foreach (var pendingChanges in pendingMembershipsToRenew)
            {
                try
                {
                    await ChangePricePlan(pendingChanges.Id, pendingChanges.CurrentPricePlanId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                }
            }
        }

        private async Task ChangePricePlan(string userId, string newPricePlanId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newPricePlanId))
                throw new Exception($"Unable to change the membership as either UserId: '{userId}' is missing or NewPricePlan '{newPricePlanId}' is missing");

            var user = await _userRepository.GetUserById(userId, cancellationToken);
            if (user == null)
                throw new Exception($"User does with '{userId}' identifier does not exists.");

            var newPricePlan = await _pricePlanRepository.GetPricePlanById(newPricePlanId, cancellationToken);
            if (newPricePlan == null)
                throw new Exception($"Unable to find the price plan with '{newPricePlanId}' identifier.");

            if (!newPricePlan.Price.Equals(0))
            {
                var paymentIntent = await _paymentService.CollectPayment(user.PaymentCustomerId, newPricePlan.Price, cancellationToken);
                user.PaymentIntentId = paymentIntent.Id;

                _logger.Information($"Collected payment of '{paymentIntent.Amount}' in minimum unit for user '{user.Id}'.");

                var paymentDao = new ProcessedPaymentDao
                {
                    UserId = user.Id,
                    PaymentServiceId = paymentIntent.Id,
                    Amount = paymentIntent.Amount,
                    PricePlanId = newPricePlan.Id,
                    NoOfStamps = newPricePlan.NoOfStamps,
                    Created = paymentIntent.Created,
                };

                await _paymentRepository.CreatePaymentReceived(paymentDao, cancellationToken);
            }

            _logger.Information($"Previous remaining stamp for the user '{user.Id}' is '{user.RemainingTimeStamps}'.");

            user.CurrentPricePlanId = newPricePlan.Id;
            user.PendingPricePlanId = null;
            user.RemainingTimeStamps = newPricePlan.NoOfStamps;
            user.MembershipRenewDate = user.MembershipRenewDate.AddMonths(1);
            user.MembershipRenewEpoch = user.MembershipRenewDate.Date.ToEpoch();

            await _userRepository.UpdateUser(user, cancellationToken);
        }
    }
}