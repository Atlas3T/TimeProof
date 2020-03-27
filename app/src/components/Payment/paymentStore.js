/* eslint-disable prefer-destructuring */
/* eslint-disable consistent-return */
/* eslint-disable camelcase */
/* eslint-disable no-await-in-loop */
/* eslint-disable class-methods-use-this */
/* eslint-disable no-restricted-syntax */

import axios from 'axios';
import config from './config';
import User from '../../store/User';
import Address from '../../store/Address';

class Store {
  constructor() {
    this.lineItems = [];
    this.products = {};
    this.productsFetchPromise = null;
  }

  // Retrieve the configuration from the API.
  async getConfig() {
    try {
      // const response = await fetch('/config');
      // const config = await response.json();
      if (config.stripe.publishableKey.includes('live')) {
        // Hide the demo notice if the publishable key is in live mode.
        document.querySelector('#order-total .demo').style.display = 'none';
      }
      return config;
    } catch (err) {
      return { error: err.message };
    }
  }

  // Create the PaymentIntent with the cart details.
  async createPaymentIntent(currency, items) {
    try {
      const response = await fetch('/payment_intents', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          currency,
          items,
        }),
      });
      const data = await response.json();
      if (data.error) {
        return { error: data.error };
      }
      return data;
    } catch (err) {
      return { error: err.message };
    }
  }

  // Create the PaymentIntent with the cart details.
  async updatePaymentIntentWithShippingCost(
    paymentIntent,
    items,
    shippingOption,
  ) {
    try {
      const response = await fetch(
        `/payment_intents/${paymentIntent}/shipping_change`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            shippingOption,
            items,
          }),
        },
      );
      const data = await response.json();
      if (data.error) {
        return { error: data.error };
      }
      return data;
    } catch (err) {
      return { error: err.message };
    }
  }

  // Format a price (assuming a two-decimal currency like EUR or USD for simplicity).
  formatPrice(amount, currency) {
    const price = amount.toFixed(2);
    const numberFormat = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      currencyDisplay: 'symbol',
    });
    return numberFormat.format(price);
  }

  async updatePaymentAddress(user, data) {
    let addressFound = false;

    if (user.accountIdentifier) {
      const addressResult = Address.query()
        .where('accountIdentifier', user.accountIdentifier).get();

      console.log('ADDDDRESSSSSSS');
      console.log(addressResult);

      // if (!Array.isArray(addressResult) || !addressResult.length) {
      let userResult = await User.insertOrUpdate({
        data: {
          accountIdentifier: user.accountIdentifier,
          address: {
            accountIdentifier: user.accountIdentifier,
            line1: data.line,
            line2: '',
            city: data.city,
            state: data.state,
            postcode: data.postalCode,
            country: data.country,
          },
        },
      });

      console.log('User updated for address');
      console.log(userResult);

      userResult = User.query().whereId(user.accountIdentifier).with('address').get();

      if (Array.isArray(userResult) && userResult.length && userResult[0].address) {
        user = userResult[0];
        console.log('Address added to user');
        addressFound = true;
      }
      // } else {
      //   addressFound = true;
      // }
    }

    return addressFound;
  }

  async makePayment(userId, paymentMethodId, amount, email) {
    console.log('BEFORE PAYMENT');
    console.log({
      userId, paymentMethodId, amount, email,
    });
    const paymentResult = await axios.post(`${process.env.API}/user/payment`, {
      userId, paymentMethodId, amount: amount * 100, email,
    });
    console.log('AFTER PAYMENT RESULT');
    console.log(paymentResult);

    return paymentResult;
  }
}

export default new Store();
