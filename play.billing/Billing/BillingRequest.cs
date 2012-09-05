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
using Android.App;
using Android.OS;
using Android.Util;

namespace play.billing
{
	   /**
     * The base class for all requests that use the MarketBillingService.
     * Each derived class overrides the run() method to call the appropriate
     * service interface.  If we are already connected to the MarketBillingService,
     * then we call the run() method directly. Otherwise, we bind
     * to the service and save the request on a queue to be run later when
     * the service is connected.
     */
    public abstract class BillingRequest 
	{
        private int mStartId;
        protected long mRequestId;

		public BillingService Service { get; set; }

        public BillingRequest(int startId) 
		{
            mStartId = startId;
        }

        public int getStartId() 
		{
            return mStartId;
        }
		
		public abstract long Run(com.android.vending.billing.IMarketBillingService service);
		

        /**
         * This is called when Android Market sends a response code for this
         * request.
         * @param responseCode the response code
         */
        public virtual void responseCodeReceived(Consts.ResponseCode responseCode) 
		{
        }

        protected Bundle makeRequestBundle(string method) 
		{
            var request = new Bundle();
            request.PutString(Consts.BILLING_REQUEST_METHOD, method);
            request.PutInt(Consts.BILLING_REQUEST_API_VERSION, 2);
			request.PutString(Consts.BILLING_REQUEST_PACKAGE_NAME, Service.PackageName);
            return request;
        }

        protected void logResponseCode(String method, Bundle response) 
		{
            var responseCode = (Consts.ResponseCode)(response.GetInt(Consts.BILLING_RESPONSE_RESPONSE_CODE));
            
			if (Consts.DEBUG) 
                Log.Error("BillingService", method + " received " + responseCode.ToString());            
        }

		public virtual void OnRemoteException() { }
    }
}