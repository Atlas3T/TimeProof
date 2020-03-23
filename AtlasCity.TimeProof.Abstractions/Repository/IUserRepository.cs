﻿using System.Threading;
using System.Threading.Tasks;
using AtlasCity.TimeProof.Abstractions.DAO;

namespace AtlasCity.TimeProof.Abstractions.Repository
{
    public interface IUserRepository
    {
        Task<UserDao> GetUserByEmail(string email, CancellationToken cancellationToken);

        Task<UserDao> CreateUser(UserDao user, CancellationToken cancellationToken);
    }
}
