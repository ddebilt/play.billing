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
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace play.billing
{
	/**
		 * Requests that the given item be offered to the user for purchase. When
		 * the purchase succeeds (or is canceled) the {@link BillingReceiver}
		 * receives an intent with the action {@link Consts#ACTION_NOTIFY}.
		 * Returns false if there was an error trying to connect to Android Market.
		 * @param productId an identifier for the item being offered for purchase
		 * @param itemType  Either Consts.ITEM_TYPE_INAPP or Consts.ITEM_TYPE_SUBSCRIPTION, indicating
		 *                  the type of item type support is being checked for.
		 * @param developerPayload a payload that is associated with a given
		 * purchase, if null, no payload is sent
		 * @return false if there was an error connecting to Android Market
	*/
	public class RequestPurchase : BillingRequest 
	{
        public string mProductId;
        public string mDeveloperPayload;
        public string mProductType;

        /** Constructor
         *
         * @param itemId  The ID of the item to be purchased. Will be assumed to be a one-time
         *                purchase.
         * @param itemType  Either Consts.ITEM_TYPE_INAPP or Consts.ITEM_TYPE_SUBSCRIPTION,
         *                  indicating the type of item type support is being checked for.
         * @param developerPayload Optional data.
         */
        public RequestPurchase(string itemId, string itemType = null, string developerPayload = null) : base(-1) {
            // This object is never created as a side effect of starting this
            // service so we pass -1 as the startId to indicate that we should
            // not stop this service after executing this request.
            mProductId = itemId;
            mDeveloperPayload = developerPayload;
            mProductType = itemType;
        }

		public override long Run(com.android.vending.billing.IMarketBillingService service)
		{
            Bundle request = makeRequestBundle("REQUEST_PURCHASE");
            request.PutString(Consts.BILLING_REQUEST_ITEM_ID, mProductId);
            request.PutString(Consts.BILLING_REQUEST_ITEM_TYPE, mProductType);
            // Note that the developer payload is optional.
            if (mDeveloperPayload != null) {
                request.PutString(Consts.BILLING_REQUEST_DEVELOPER_PAYLOAD, mDeveloperPayload);
            }

            Bundle response = service.SendBillingRequest(request);
						
            PendingIntent pendingIntent
                    = response.GetParcelable(Consts.BILLING_RESPONSE_PURCHASE_INTENT) as PendingIntent;
            
			if (pendingIntent == null) 
			{
                Log.Error("BillingService", "Error with requestPurchase");
                return Consts.BILLING_RESPONSE_INVALID_REQUEST_ID;
            }

            Intent intent = new Intent();
            ResponseHandler.buyPageIntentResponse(pendingIntent, intent);
            return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID,
                    Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
        }

		public override void responseCodeReceived(Consts.ResponseCode responseCode) 
		{
			ResponseHandler.responseCodeReceived(this.Service, this, responseCode);
        }
    }
}