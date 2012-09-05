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
using Android.Util;

namespace play.billing
{
	   /**
     * Wrapper class that checks if in-app billing is supported.
     *
     * Note: Support for subscriptions implies support for one-time purchases. However, the opposite
     * is not true.
     *
     * Developers may want to perform two checks if both one-time and subscription products are
     * available.
     */
    class CheckBillingSupported : BillingRequest 
	{
		string mItemType;

        /** Constructor
         *
         * Note: Support for subscriptions implies support for one-time purchases. However, the
         * opposite is not true.
         *
         * Developers may want to perform two checks if both one-time and subscription products are
         * available.
         *
         * @pram itemType Either Consts.ITEM_TYPE_INAPP or Consts.ITEM_TYPE_SUBSCRIPTION, indicating
         * the type of item support is being checked for.
         */
        public CheckBillingSupported(string itemType = null) : base(-1) 
		{
			mItemType = itemType;
		}

		public override long Run(com.android.vending.billing.IMarketBillingService service) 
		{
            Bundle request = makeRequestBundle("CHECK_BILLING_SUPPORTED");

			if (mItemType != null)
				request.PutString(Consts.BILLING_REQUEST_ITEM_TYPE, mItemType);
			
            Bundle response = service.SendBillingRequest(request);
            int responseCode = response.GetInt(Consts.BILLING_RESPONSE_RESPONSE_CODE);
            
			if (Consts.DEBUG) 
                Log.Info("BillingService", "CheckBillingSupported response code: " + responseCode.ToString());
                        
			bool billingSupported = (responseCode == (int)Consts.ResponseCode.RESULT_OK);
            ResponseHandler.checkBillingSupportedResponse(billingSupported);
            return Consts.BILLING_RESPONSE_INVALID_REQUEST_ID;
        }
    }
}