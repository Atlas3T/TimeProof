<template>
  <q-page class="flex flex-center text-secondary">
    <div
      v-if="isLoggedIn"
      class="q-my-lg"
      style="width:82vw;"
    >
      <div
        class="q-gutter-x-lg q-mb-md"
      >
        <div class="col-auto">
          <div class="row text-h6 text-weight-bold">
            <q-icon
              class="icon-spacing q-mr-sm"
              name="fas fa-tachometer-alt"
              size="1.25rem"
            />
            Usage Summary
          </div>
          <Usage />
        </div>
      </div>
      <div>
        <Timestamps />
      </div>
    </div>
    <div
      v-else
      flat
      class="q-pa-xl flex flex-center column text-center"
    >
      <div class="text-h6 text-weight-bold text-grey-6">
        {{ $t('notSignedIn') }}
      </div>
      <q-btn
        id="pagesDashboardSignUpSignInBtn"
        unelevated
        flat
        :label="$t('signUpSignIn')"
        class="q-mt-md shade-color"
        @click="$auth.signIn()"
      />
    </div>
    <q-dialog
      v-if="user"
      v-model="user.firstTimeDialog"
    >
      <CreateFirstTimestampPopup
        @closeDialog="closeTimestampDialog"
      />
    </q-dialog>
  </q-page>
</template>

<script>
import Timestamps from '../components/Timestamps';
import Usage from '../components/Usage';
import User from '../store/User';
import CreateFirstTimestampPopup from '../components/CreateFirstTimestampPopup';

export default {
  name: 'Dashboard',
  components: {
    Timestamps,
    Usage,
    CreateFirstTimestampPopup,
  },

  data() {
    return {
      firstTimeDialog: false,
    };
  },

  computed: {
    isLoggedIn() {
      return !!this.account;
    },
    account() {
      return this.$auth.account();
    },
    user() {
      return this.$auth.user();
    },
  },

  methods: {
    async closeTimestampDialog() {
      await User.update({
        data: {
          accountIdentifier: this.account.accountIdentifier,
          firstTimeDialog: false,
        },
      });
    },
  },
};
</script>
<style lang="scss" scoped>
.verify .q-card {
  padding: 0;
}
.icon-spacing {
  margin-top: 4px;
}
</style>
