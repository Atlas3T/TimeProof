<template>
  <div>
    <q-card
      flat
      class="recent-timestamps"
    >
      <div class="row text-weight-bold text-h6">
        {{ $t('recentTimestamps') }}
      </div>
      <div class="row">
        <div
          class="col-7"
        >
          {{ $t('file') }}
        </div>
        <div>{{ $t('date') }}</div>
      </div>
      <div
        v-for="stamp in timestamps"
        :key="stamp.txId"
        class="row q-py-sm stamp-item"
        @click="timestampDialog(stamp)"
      >
        <div
          class="col-7 q-pr-sm overflow"
        >
          <q-icon
            class="col-auto text-grey-6 q-pr-sm"
            :name="fileIcon(stamp.type)"
            style="font-size: 1.5em"
          />
          {{ stamp.name }}
        </div>
        <div class="text-right">
          {{ getDate(stamp.date) }}
        </div>
      </div>
      <div
        class="text-center text-blue"
        @click="$emit('open')"
      >
        {{ $t('viewAll') }}
      </div>
    </q-card>
    <q-dialog v-model="confirmed">
      <q-card>
        <div class="row justify-end">
          <q-icon
            v-close-popup
            size="md"
            name="close"
          />
        </div>
        <Proof
          v-if="confirmed"
          :proof="file"
          :scope="{dialog: true}"
        />
      </q-card>
    </q-dialog>
  </div>
</template>
<script>
import Proof from '../Proof';
import { fileIcon } from '../../util';

export default {
  name: 'RecentTimestamps',

  components: {
    Proof,
  },

  data() {
    return {
      confirmed: false,
      file: null,
      fileIcon,
    };
  },

  computed: {
    account() {
      return this.$auth.account();
    },

    user() {
      return this.$auth.user(false, true, 'timestamps');
    },
    timestamps() {
      const { timestamps } = this.user;
      const ts = timestamps.slice(0);

      ts.sort((a, b) => new Date(b.date) - new Date(a.date));

      if (ts.length > 5) {
        return ts.slice(0, 5);
      }
      return ts.slice(0);
    },
  },

  methods: {
    timestampDialog(stamp) {
      this.file = stamp;
      this.confirmed = true;
    },

    getDate(time) {
      const date = new Date(time);
      return `${date.toLocaleDateString()} ${date.toLocaleTimeString('en-GB')}`;
    },
  },
};
</script>
<style lang="scss">
.recent-timestamps {
    width: 25em;
}
.stamp-item {
  border-top: 1px solid $grey-4;
}

.stamp-item:hover {
  background: $grey-2;
}

.overflow {
  text-overflow: ellipsis;
  overflow: hidden;
  white-space: nowrap;
}
</style>
