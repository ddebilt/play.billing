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
     * Wrapper class that confirms a list of notifications to the server.
     */
	/**
	 * Confirms receipt of a purchase state change. Each {@code notifyId} is
	 * an opaque identifier that came from the server. This method sends those
	 * identifiers back to the MarketBillingService, which ACKs them to the
	 * server. Returns false if there was an error trying to connect to the
	 * MarketBillingService.
	 * @param startId an identifier for the invocation instance of this service
	 * @param notifyIds a list of opaque identifiers associated with purchase
	 * state changes.
	 * @return false if there was an error connecting to Market
	 */
    class ConfirmNotifications : BillingRequest {
        string[] mNotifyIds;

        public ConfirmNotifications(int startId, string[] notifyIds) : base(startId) {
            mNotifyIds = notifyIds;
        }

		public override long Run(com.android.vending.billing.IMarketBillingService service)
		{
            Bundle request = makeRequestBundle("CONFIRM_NOTIFICATIONS");
            request.PutStringArray(Consts.BILLING_REQUEST_NOTIFY_IDS, mNotifyIds);
			Bundle response = service.SendBillingRequest(request);
            logResponseCode("confirmNotifications", response);
            return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID,
                    Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
        }
    }
}