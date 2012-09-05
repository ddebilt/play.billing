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
using Android.Widget;

namespace play.billing
{
	public interface IPurchaseObserver
	{		
		/**
		 * This is the callback that is invoked when Android Market responds to the
		 * {@link BillingService#checkBillingSupported()} request.
		 * @param supported true if in-app billing is supported.
		 */
		void onBillingSupported(bool supported);

		/**
		 * This is the callback that is invoked when an item is purchased,
		 * refunded, or canceled.  It is the callback invoked in response to
		 * calling {@link BillingService#requestPurchase(String)}.  It may also
		 * be invoked asynchronously when a purchase is made on another device
		 * (if the purchase was for a Market-managed item), or if the purchase
		 * was refunded, or the charge was canceled.  This handles the UI
		 * update.  The database update is handled in
		 * {@link ResponseHandler#purchaseResponse(Context, PurchaseState,
		 * String, String, long)}.
		 * @param purchaseState the purchase state of the item
		 * @param itemId a string identifying the item (the "SKU")
		 * @param quantity the current quantity of this item after the purchase
		 * @param purchaseTime the time the product was purchased, in
		 * milliseconds since the epoch (Jan 1, 1970)
		 */
		void onPurchaseStateChange(Consts.PurchaseState purchaseState, string itemId, int quantity, long purchaseTime, string developerPayload);

		/**
		 * This is called when we receive a response code from Market for a
		 * RequestPurchase request that we made.  This is NOT used for any
		 * purchase state changes.  All purchase state changes are received in
		 * {@link #onPurchaseStateChange(PurchaseState, String, int, long)}.
		 * This is used for reporting various errors, or if the user backed out
		 * and didn't purchase the item.  The possible response codes are:
		 *   RESULT_OK means that the order was sent successfully to the server.
		 *       The onPurchaseStateChange() will be invoked later (with a
		 *       purchase state of PURCHASED or CANCELED) when the order is
		 *       charged or canceled.  This response code can also happen if an
		 *       order for a Market-managed item was already sent to the server.
		 *   RESULT_USER_CANCELED means that the user didn't buy the item.
		 *   RESULT_SERVICE_UNAVAILABLE means that we couldn't connect to the
		 *       Android Market server (for example if the data connection is down).
		 *   RESULT_BILLING_UNAVAILABLE means that in-app billing is not
		 *       supported yet.
		 *   RESULT_ITEM_UNAVAILABLE means that the item this app offered for
		 *       sale does not exist (or is not published) in the server-side
		 *       catalog.
		 *   RESULT_ERROR is used for any other errors (such as a server error).
		 */
		void onRequestPurchaseResponse(RequestPurchase request, Consts.ResponseCode responseCode);

		/**
		 * This is called when we receive a response code from Android Market for a
		 * RestoreTransactions request that we made.  A response code of
		 * RESULT_OK means that the request was successfully sent to the server.
		 */
		void onRestoreTransactionsResponse(RestoreTransactions request, Consts.ResponseCode responseCode);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="pendingIntent"></param>
		/// <param name="intent"></param>
		void startBuyPageActivity(PendingIntent pendingIntent, Intent intent);
	}




	public class PurchaseObserver : IPurchaseObserver
	{
		Activity m_activity;

		public PurchaseObserver(Activity activity)
		{
			m_activity = activity;
		}

		public void onBillingSupported(bool supported)
		{
			Toast.MakeText(m_activity, string.Format("Billing is {0}supported.", supported ? string.Empty : "not "), ToastLength.Long).Show();
		}

		public void onPurchaseStateChange(Consts.PurchaseState purchaseState, string itemId, int quantity, long purchaseTime, string developerPayload) //bool fromBackgroundTHread = false)
		{
			m_activity.RunOnUiThread(() =>
				{
					Toast.MakeText(m_activity, string.Format("PurchaseState: {0}", purchaseState.ToString()), ToastLength.Long).Show();
				});
		}

		public void onRequestPurchaseResponse(RequestPurchase request, Consts.ResponseCode responseCode)
		{
			Toast.MakeText(m_activity, string.Format("Request response code: {0}", responseCode.ToString()), ToastLength.Long).Show();
		}

		public void onRestoreTransactionsResponse(RestoreTransactions request, Consts.ResponseCode responseCode)
		{
			Toast.MakeText(m_activity, string.Format("Restore response code: {0}", responseCode.ToString()), ToastLength.Long).Show();
		}

		public void startBuyPageActivity(PendingIntent pendingIntent, Intent intent)
		{
			m_activity.StartIntentSender(pendingIntent.IntentSender, intent, 0, 0, 0);
		}
	}
}