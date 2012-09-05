/*
 * Copyright (C) 2010 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using Android.OS;

namespace play.billing
{
	/**
	 * Gets the purchase information. This message includes a list of
	 * notification IDs sent to us by Android Market, which we include in
	 * our request. The server responds with the purchase information,
	 * encoded as a JSON string, and sends that to the {@link BillingReceiver}
	 * in an intent with the action {@link Consts#ACTION_PURCHASE_STATE_CHANGED}.
	 * Returns false if there was an error trying to connect to the MarketBillingService.
	 *
	 * @param startId an identifier for the invocation instance of this service
	 * @param notifyIds a list of opaque identifiers associated with purchase
	 * state changes
	 * @return false if there was an error connecting to Android Market
	 */
	public class GetPurchaseInformation : BillingRequest 
	{
        long mNonce;
        string[] mNotifyIds;

        public GetPurchaseInformation(int startId, string[] notifyIds) : base(startId) 
		{
            mNotifyIds = notifyIds;
        }

		public override long Run(com.android.vending.billing.IMarketBillingService service)
		{
            mNonce = Security.generateNonce();

            Bundle request = makeRequestBundle("GET_PURCHASE_INFORMATION");
            request.PutLong(Consts.BILLING_REQUEST_NONCE, mNonce);
            request.PutStringArray(Consts.BILLING_REQUEST_NOTIFY_IDS, mNotifyIds);
            Bundle response = service.SendBillingRequest(request);
            logResponseCode("getPurchaseInformation", response);
            return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID,
                    Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
        }

		public override void OnRemoteException()
		{
            Security.removeNonce(mNonce);
        }
    }
}