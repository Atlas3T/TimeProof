/* eslint-disable prefer-destructuring */
/* eslint-disable no-undef */
import puppeteer from 'puppeteer';

jest.setTimeout(1000000);

const clickButton = async (page, name, timeout, multiple) => {
  const buttonSelector = `button[data-test-key=${name}]`;

  await page.waitForSelector(buttonSelector, { visible: true, timeout });
  let button;
  if (!multiple) {
    button = await page.$(buttonSelector);
  } else {
    const buttons = await page.$$(buttonSelector);
    if (Array.isArray(buttons) && buttons.length > 1) {
      button = buttons[1];
    }
  }

  if (name === 'paymentButton') {
    expect(button).not.toBeNull();
    expect(await button.evaluate(node => node.innerText)).toBe('Pay £4.99');
  }

  await page.evaluate(el => el.click(), button);
};

const changeText = async (page, name, content, internal, timeout, delay) => {
  const inputSelector = internal ? `input[data-test-key=${name}]` : name;

  const input = await page.waitForSelector(inputSelector, { visible: true, timeout });
  await input.focus();
  await input.type(content, { delay });
};

const selectOption = async (page, name, content, internal, timeout) => {
  const selectSelector = internal ? `select[data-test-key=${name}]` : name;

  const dropdown = await page.waitForSelector(selectSelector, { visible: true, timeout });
  await dropdown.focus();
  await dropdown.select(content);
};

const getFrame = async (page, frameName, timeout) => {
  const frameSelector = `iframe[name=${frameName}]`;

  const elementHandle = await page.waitForSelector(frameSelector, { visible: true, timeout });
  const frame = await elementHandle.contentFrame();

  return frame;
};

const changeTextInFrame = async (frame, contentSelector, content, timeout, delay) => {
  const input = await frame.waitForSelector(contentSelector, { visible: true, timeout });
  await input.type(content, { delay });
};

test('Payment e2e test scenerio 1', async () => {
  const browser = await puppeteer.launch({
    headless: false,
    slowMo: 5,
    args: [
      '--disable-web-security',
      '--window-size=1280,800',
      '--disable-features=IsolateOrigins,site-per-process',
    ],
  });
  const page = await browser.newPage();
  const app = 'http://localhost:6420';
  await page.goto(app);

  await changeText(page, 'input#logonIdentifier', 'veyseltosun.vt+1@gmail.com', false, 10000, 10);

  await page.click('input#password');
  await page.type('input#password', 'BiCo2288!');
  await page.click('button#next');

  // TO DO when user hasn't a key make this control
  await page.waitForSelector('button[data-test-key="createYourSigningKey"]',
    { visible: true, timeout: 10000 });

  const createYourSigningKey = await page.$('button[data-test-key="createYourSigningKey"]');

  if (createYourSigningKey) {
    expect(page.url()).toEqual(expect.stringContaining('new-key'));
    // await createYourSigningKey.click();
    await page.evaluate(el => el.click(), createYourSigningKey);

    await changeText(page, 'encryptKeyPassword', 'BiCo2288!', true, 10000, 100);// enter new  secret key
    await clickButton(page, 'newKeyContinue', 10000, false);// Continue
    await clickButton(page, 'saveKeyDownloadKeystore', 10000, false); // download the keystore
    await clickButton(page, 'newKeyContinue', 10000, false); // Continue
    await clickButton(page, 'newKeyContinue', 10000, false); // Go To DashBoard
    await clickButton(page, 'cancelCreateFirstTimeStamp', 10000, false); // Cancel Create Timestamp Popup -- go dashboard
  }

  await clickButton(page, 'upgradeButton', 10000, false); // Upgrade the plan
  await clickButton(page, 'choosePlanButton', 10000, true); // Choose a plan

  // Billing details
  await selectOption(page, 'paymentBillingCountry', 'GB', true, 10000);// payment feilds: country
  await changeText(page, 'paymentBillingName', 'Veysel TOSUN', true, 10000, 20);// payment feilds: name
  await changeText(page, 'paymentBillingAddress', '75 Linden Avenue Wembley', true, 10000, 20);// payment feilds: address
  await changeText(page, 'paymentBillingCity', 'London', true, 10000, 20);// payment feilds: city
  await changeText(page, 'paymentBillingState', 'London', true, 10000, 20);// payment feilds: state
  await changeText(page, 'paymentBillingPostCode', 'HA9 8BB', true, 10000, 20);// payment feilds: post code

  // card details
  const frame = await getFrame(page, '__privateStripeFrame5', 10000);

  await changeTextInFrame(frame, 'input[name="cardnumber"]', '4242424242424242', 10000, 50);// payment feilds: cardnumber
  await changeTextInFrame(frame, 'input[name="exp-date"]', '1223', 10000, 50);// payment feilds: exp-date
  await changeTextInFrame(frame, 'input[name="cvc"]', '233', 10000, 50);// payment feilds: cvc
  await changeTextInFrame(frame, 'input[name="postal"]', '56789', 10000, 50);// payment feilds: postal

  await clickButton(page, 'paymentButton', 10000, false);// make payment

  await browser.close();
});