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
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using com.android.vending.billing;
using Android.Util;

namespace play.billing
{
 /**
 * This class sends messages to Android Market on behalf of the application by
 * connecting (binding) to the MarketBillingService. The application
 * creates an instance of this class and invokes billing requests through this service.
 *
 * The {@link BillingReceiver} class starts this service to process commands
 * that it receives from Android Market.
 *
 * You should modify and obfuscate this code before using it.
 */
	public class BillingService : Service, IServiceConnection
	{
		private const string TAG = "BillingService";

		/** The service connection to the remote MarketBillingService. */
		private static IMarketBillingService mService;

		/**
		 * The list of requests that are pending while we are waiting for the
		 * connection to the MarketBillingService to be established.
		 */
		public static List<BillingRequest> PendingRequests = new List<BillingRequest>();

		/**
		 * The list of requests that we have sent to Android Market but for which we have
		 * not yet received a response code. The HashMap is indexed by the
		 * request Id that each request receives when it executes.
		 */
		private static Dictionary<long, BillingRequest> mSentRequests = new Dictionary<long, BillingRequest>();

		public BillingService() : base() { }

		public void setContext(Context context)
		{
			AttachBaseContext(context);
		}

		/**
		 * We don't support binding to this service, only starting the service.
		 */
		public override IBinder OnBind(Intent intent)
		{
			return null;
		}

		public override void OnStart(Intent intent, int startId)
		{
			handleCommand(intent, startId);
		}

		/**
		 * The {@link BillingReceiver} sends messages to this service using intents.
		 * Each intent has an action and some extra arguments specific to that action.
		 * @param intent the intent containing one of the supported actions
		 * @param startId an identifier for the invocation instance of this service
		 */
		public void handleCommand(Intent intent, int startId)
		{
			string action = intent.Action;

			if (Consts.DEBUG)
				Log.Info(TAG, "handleCommand() action: " + action);
			
			if (Consts.ACTION_CONFIRM_NOTIFICATION.Equals(action))
			{
				string[] notifyIds = intent.GetStringArrayExtra(Consts.NOTIFICATION_ID);
				Execute(new ConfirmNotifications(startId, notifyIds));
			}
			else if (Consts.ACTION_GET_PURCHASE_INFORMATION.Equals(action))
			{
				string notifyId = intent.GetStringExtra(Consts.NOTIFICATION_ID);
				Execute(new GetPurchaseInformation(startId, new string[] { notifyId }));
			}
			else if (Consts.ACTION_PURCHASE_STATE_CHANGED.Equals(action))
			{
				string signedData = intent.GetStringExtra(Consts.INAPP_SIGNED_DATA);
				string signature = intent.GetStringExtra(Consts.INAPP_SIGNATURE);
				purchaseStateChanged(startId, signedData, signature);
			}
			else if (Consts.ACTION_RESPONSE_CODE.Equals(action))
			{
				long requestId = intent.GetLongExtra(Consts.INAPP_REQUEST_ID, -1);
				int responseCodeIndex = intent.GetIntExtra(Consts.INAPP_RESPONSE_CODE, (int)Consts.ResponseCode.RESULT_ERROR);
				Consts.ResponseCode responseCode = (Consts.ResponseCode)responseCodeIndex;
				checkResponseCode(requestId, responseCode);
			}
		}

		/**
		 * Binds to the MarketBillingService and returns true if the bind
		 * succeeded.
		 * @return true if the bind succeeded; false otherwise
		 */
		bool bindToMarketBillingService()
		{
			try
			{
				if (Consts.DEBUG)
					Log.Info(TAG, "binding to Market billing service");
				
				var bindResult = this.BindService(new Intent(Consts.MARKET_BILLING_SERVICE_ACTION), this, Bind.AutoCreate);

				if (bindResult)
					return true;				
				else
					Log.Error(TAG, "Could not bind to service.");
			}
			catch (System.Security.SecurityException e)
			{
				Log.Error(TAG, "Security exception: " + e);
			}
			return false;
		}



		

		public bool CheckBillingSupported(string itemType = null)
		{
			return Execute(new CheckBillingSupported(itemType));
		}

		public bool ConfirmNotifications(int startId, string[] notifyIds)
		{
			return Execute(new ConfirmNotifications(startId, notifyIds));
		}

		public bool GetPurchaseInformation(int startId, string[] notifyIds)
		{
			return Execute(new GetPurchaseInformation(startId, notifyIds));
		}

		public bool RequestPurchase(string itemId, string itemType = null, string developerPayload = null)
		{
			return Execute(new RequestPurchase(itemId, itemType, developerPayload));
		}

		public bool RestoreTransactions()
		{
			return Execute(new RestoreTransactions());
		}


			
		/// <summary>
		/// Executes a BillingRequest
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		bool Execute(BillingRequest request)
		{
			request.Service = this;

			if (mService != null)
			{
				try
				{
					var requestId = request.Run(mService);
					mSentRequests.Add(requestId, request);
					return true;
				}
				catch (RemoteException)
				{
					mService = null;
					request.OnRemoteException();
				}
			}
			else
			{
				PendingRequests.Add(request);
				return bindToMarketBillingService();
			}

			return false;
		}		

		/**
		 * Verifies that the data was signed with the given signature, and calls
		 * {@link ResponseHandler#purchaseResponse(Context, PurchaseState, string, string, long)}
		 * for each verified purchase.
		 * @param startId an identifier for the invocation instance of this service
		 * @param signedData the signed JSON string (signed, not encrypted)
		 * @param signature the signature for the data, signed with the private key
		 */
		private void purchaseStateChanged(int startId, string signedData, string signature)
		{
			List<Security.VerifiedPurchase> purchases;
			purchases = Security.verifyPurchase(signedData, signature);
			if (purchases == null)
			{
				return;
			}

			List<string> notifyList = new List<string>();
			foreach (play.billing.Security.VerifiedPurchase vp in purchases)
			{
				if (vp.notificationId != null)
				{
					notifyList.Add(vp.notificationId);
				}
				ResponseHandler.purchaseResponse(this, vp.purchaseState, vp.productId,
						vp.orderId, vp.purchaseTime, vp.developerPayload);
			}
			if (notifyList.Count > 0)
			{
				string[] notifyIds = notifyList.ToArray();
				Execute(new ConfirmNotifications(startId, notifyIds));
			}
		}

		/**
		 * This is called when we receive a response code from Android Market for a request
		 * that we made. This is used for reporting various errors and for
		 * acknowledging that an order was sent to the server. This is NOT used
		 * for any purchase state changes.  All purchase state changes are received
		 * in the {@link BillingReceiver} and passed to this service, where they are
		 * handled in {@link #purchaseStateChanged(int, string, string)}.
		 * @param requestId a number that identifies a request, assigned at the
		 * time the request was made to Android Market
		 * @param responseCode a response code from Android Market to indicate the state
		 * of the request
		 */
		private void checkResponseCode(long requestId, Consts.ResponseCode responseCode)
		{
			BillingRequest request = mSentRequests[requestId];
			if (request != null)
			{
				if (Consts.DEBUG)
					Log.Debug(TAG, request.GetType().Name + ": " + responseCode);
				
				request.responseCodeReceived(responseCode);
			}
			mSentRequests.Remove(requestId);
		}

		/**
		 * Runs any pending requests that are waiting for a connection to the
		 * service to be established.  This runs in the main UI thread.
		 */
		private void runPendingRequests()
		{
			int maxStartId = -1;
			BillingRequest request;

			while (PendingRequests.Count > 0)
			{
				if (mService != null)
				{
					request = PendingRequests[0];

					if (Execute(request))
					{
						PendingRequests.RemoveAt(0);
						if (maxStartId < request.getStartId())
							maxStartId = request.getStartId();
					}
				}
				else
				{
					bindToMarketBillingService();
					return;
				}
			}

			// If we get here then all the requests ran successfully.  If maxStartId
			// is not -1, then one of the requests started the service, so we can
			// stop it now.
			if (maxStartId >= 0)
			{
				if (Consts.DEBUG)
				{
					Log.Info(TAG, "stopping service, startId: " + maxStartId);
				}
				StopSelf(maxStartId);
			}
		}

		/**
		 * This is called when we are connected to the MarketBillingService.
		 * This runs in the main UI thread.
		 */
		public void OnServiceConnected(ComponentName name, IBinder service)
		{
			if (Consts.DEBUG)
			{
				Log.Debug(TAG, "Billing service connected");
			}
			mService = BillingServiceStub.AsInterface(service);
			runPendingRequests();
		}

		/**
		 * This is called when we are disconnected from the MarketBillingService.
		 */
		public void OnServiceDisconnected(ComponentName name)
		{
			Log.Warn(TAG, "Billing service disconnected");
			mService = null;
		}

		/**
		 * Unbinds from the MarketBillingService. Call this when the application
		 * terminates to avoid leaking a ServiceConnection.
		 */
		public void unbind()
		{
			try
			{
				UnbindService(this);
			}
			catch (Java.Lang.IllegalArgumentException)
			{
				// This might happen if the service was disconnected
			}
		}
	}
}