<template>
  <q-card class="q-pa-lg q-gutter-y-sm justify-center">
    <div class="row text-h6 text-weight-boldtext-center justify-center">
      {{ $t('saveKeystore') }}
    </div>
    <div class="row text-center justify-center">
      {{ $t('saveKeystoreDesc') }}
    </div>
    <div class="row justify-center">
      <q-btn
        id="keyDownloadKeyBackupBtn"
        outline
        class="shade-color"
        label="Download Keystore file"
        @click="$emit('backup')"
      />
    </div>
  </q-card>
</template>
