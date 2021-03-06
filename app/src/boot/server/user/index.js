/* eslint-disable class-methods-use-this */
import Vue from 'vue';
import Timestamp from '../../../store/Timestamp';
import User from '../../../store/User';
import Server from '../index';

class UserServer extends Server {
  async fetchTimestamps(accountIdentifier) {
    const re = /(?:\.([^.]+))?$/;

    const { data, status } = await this.axios.get(`${process.env.API}/gettimestamps`);
    if (status === 200 && data) {
      const stamps = data.map(file => ({
        id: file.id,
        txId: file.transactionId,
        status: file.status,
        hash: file.fileHash,
        signature: file.signature,
        pubKey: file.publicKey.toLowerCase(),
        accountIdentifier,
        name: file.fileName,
        date: file.timestamp,
        type: re.exec(file.fileName)[1],
        blockNumber: Number(file.blockNumber),
      }));

      await Timestamp.create({
        data: stamps,
      });
    }
  }

  async verifyUserDetails() {
    // TO DO change user address
    try {
      const user = this.getUser();
      const account = this.getAccount();

      if (account) {
        let result = await this.axios.get(`${process.env.API}/user`,
          { headers: { 'Cache-Control': 'no-cache' } });
        console.log('VERIFY USER RESULT');
        console.log(result);
        let data = {
          firstName: account.idToken.given_name,
          lastName: account.idToken.family_name,
        };
        if (!result || !result.data) {
          let address;
          if (user) {
            if (user.address) {
              address = {
                line1: user.address.line1,
                line2: user.address.line2,
                city: user.address.city,
                state: user.address.state,
                postcode: user.address.postcode,
                country: user.address.country,
              };
            }

            data = {
              email: user.email,
              firstName: user.givenName,
              lastName: user.familyName,
              address,
            };
          } else {
            const [accEmail] = account.idToken.emails;
            data.email = accEmail;
          }
          console.log(data);
          result = await this.axios.post(`${process.env.API}/user`, data);
          console.log('New User created on Timeproof API');
          console.log(result);
          if (!result && !result.data) {
            throw new Error('user data not found');
          }
        } else {
          const { status, error } = await this.axios.put(`${process.env.API}/user`, data);
          if (status === 200 && !error) {
            result.data.firstName = data.firstName;
            result.data.lastName = data.lastName;
          }
          console.log('User is already exist and update in timeproof api');
          console.log(result);
        }

        return result.data;
      }
    } catch (err) {
      console.log('verifyUserDetails error: ', err);
    }
    return Promise.resolve();
  }

  async getSetupIntent() {
    let setupIntentResult;
    console.log('BEFORE GET SETUP INTENT');
    try {
      setupIntentResult = await this.axios.get(`${process.env.API}/user/setupintent`);
      console.log('AFTER GET SETUP INTENT');
      console.log(setupIntentResult);
    } catch (err) {
      console.log('AFTER GET SETUP INTENT ERROR');
      console.error(err);
    }

    return setupIntentResult;
  }

  async refreshCard(user) {
    try {
      const { data, status } = await this.axios.get(`${process.env.API}/user/paymentmethod`);
      console.log('PAYMENT METHOD RESULT');
      console.log(data);
      if (status === 200 && data && data.card) {
        User.update({
          data: {
            accountIdentifier: user.accountIdentifier,
            selectedCardNumber: `**** **** **** ${data.card.last4}`,
            cardExpirationDate: `${data.card.expMonth} / ${data.card.expYear}`,
          },
        });
      }
    } catch (err) {
      console.log('PAYMENT METHOD RESULT ERROR');
      console.log(err);
    }
  }

  async saveCard(name, stripe, card) {
    const response = {};
    console.log('BEFORE SAVE THE CARD');

    try {
      const { data: setupIntentData, status, error } = await this.getSetupIntent();

      if (setupIntentData && status === 200 && !error) {
        const { setupIntent: confirmSetupIntent, error: confirmError } = await
        stripe.confirmCardSetup(setupIntentData.clientSecret,
          {
            payment_method: {
              card,
              billing_details: {
                name,
              },
            },
          });

        console.log('IN SAVE THE CARD');
        console.log(confirmSetupIntent);

        if (confirmSetupIntent && confirmSetupIntent.status === 'succeeded' && !confirmError) {
          response.status = confirmSetupIntent.status;
        } else if (confirmError) {
          response.error = confirmError;
        } else {
          response.error.message = 'Unexpected error';
        }
      } else if (error) {
        response.error = error;
      } else {
        response.error.message = 'Unexpected error';
      }
      console.log('AFTER SAVE THE CARD');
      console.log(response);
    } catch (err) {
      console.log('AFTER SAVE THE CARD ERROR');
      console.log(err);
      response.error.message = 'Unexpected error';
    }

    return response;
  }

  async sendKey(obj) {
    let sendKeyResult;
    console.log('BEFORE SENDING KEY');
    try {
      const axiosConfig = {
        headers: {
          'Content-Type': 'application/json;charset=UTF-8',
          'Access-Control-Allow-Origin': '*',
        },
      };
      sendKeyResult = await this.axios.post(`${process.env.API}/user/sendkey`,
        JSON.stringify(obj), axiosConfig);
      console.log('AFTER SENDING KEY');
      console.log(sendKeyResult);
    } catch (err) {
      console.log('AFTER SENDING KEY ERROR');
      console.error(err);
    }

    return sendKeyResult;
  }

  async cancelFuturePlan() {
    console.log('BEFORE CANCELLING FUTURE PLAN');
    try {
      const { status, error } = await
      this.axios.put(`${process.env.API}/ppid/cancel/${this.getUser().pendingPricePlanId}`);
      if (status === 200 && !error) {
        User.update({
          data: {
            accountIdentifier: this.getUser().accountIdentifier,
            pendingPricePlanId: '',
          },
        });
      }
      console.log('AFTER CANCELLING FUTURE PLAN');
    } catch (err) {
      console.log('AFTER CANCELLING FUTURE PLAN ERROR');
      console.error(err);
    }
  }
}

const userServer = new UserServer();

Vue.prototype.$userServer = userServer;

export default userServer;
