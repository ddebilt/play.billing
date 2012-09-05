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
using Android.OS;

namespace play.billing
{
	 /**
     * Wrapper class that sends a RESTORE_TRANSACTIONS message to the server.
     */
	/**
		 * Requests transaction information for all managed items. Call this only when the
		 * application is first installed or after a database wipe. Do NOT call this
		 * every time the application starts up.
		 * @return false if there was an error connecting to Android Market
	*/
    public class RestoreTransactions : BillingRequest 
	{
        long mNonce;

        public RestoreTransactions() : base (-1) 
		{
            // This object is never created as a side effect of starting this
            // service so we pass -1 as the startId to indicate that we should
            // not stop this service after executing this request.
        }

        public override long Run(com.android.vending.billing.IMarketBillingService service)
		{
            mNonce = Security.generateNonce();

            Bundle request = makeRequestBundle("RESTORE_TRANSACTIONS");
            request.PutLong(Consts.BILLING_REQUEST_NONCE, mNonce);
            Bundle response = service.SendBillingRequest(request);
            logResponseCode("restoreTransactions", response);
            return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID,
                    Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
        }

		public override void OnRemoteException()
		{
            Security.removeNonce(mNonce);
        }

		public override void responseCodeReceived(Consts.ResponseCode responseCode)
		{
			ResponseHandler.responseCodeReceived(this.Service, this, responseCode);
        }
    }
}