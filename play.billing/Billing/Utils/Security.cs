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
using System.Text;
using Java.Security;
using Android.Util;
using Android.Text;
using System.Json;
using Java.Security.Spec;
using Java.Lang;

namespace play.billing
{
	public class Security 
	{
		private static string TAG = "Security";

		private static string KEY_FACTORY_ALGORITHM = "RSA";
		private static string SIGNATURE_ALGORITHM = "SHA1withRSA";
		private static SecureRandom RANDOM = new SecureRandom();
		
		public static bool ExpectSignature = true;

		/**
		 * This keeps track of the nonces that we generated and sent to the
		 * server.  We need to keep track of these until we get back the purchase
		 * state and send a confirmation message back to Android Market. If we are
		 * killed and lose this list of nonces, it is not fatal. Android Market will
		 * send us a new "notify" message and we will re-generate a new nonce.
		 * This has to be "static" so that the {@link BillingReceiver} can
		 * check if a nonce exists.
		 */
		private static HashSet<long> sKnownNonces = new HashSet<long>();

		/**
		 * A class to hold the verified purchase information.
		 */
		public class VerifiedPurchase {
			public Consts.PurchaseState purchaseState;
			public string notificationId;
			public string productId;
			public string orderId;
			public long purchaseTime;
			public string developerPayload;

			public VerifiedPurchase(Consts.PurchaseState purchaseState, string notificationId,
					string productId, string orderId, long purchaseTime, string developerPayload) {
				this.purchaseState = purchaseState;
				this.notificationId = notificationId;
				this.productId = productId;
				this.orderId = orderId;
				this.purchaseTime = purchaseTime;
				this.developerPayload = developerPayload;
			}
		}

		/** Generates a nonce (a random number used once). */
		public static long generateNonce() {
			long nonce = RANDOM.NextLong();
			sKnownNonces.Add(nonce);
			return nonce;
		}

		public static void removeNonce(long nonce) {
			sKnownNonces.Remove(nonce);
		}

		public static bool isNonceKnown(long nonce) {
			return sKnownNonces.Contains(nonce);
		}

		/**
		 * Verifies that the data was signed with the given signature, and returns
		 * the list of verified purchases. The data is in JSON format and contains
		 * a nonce (number used once) that we generated and that was signed
		 * (as part of the whole data string) with a private key. The data also
		 * contains the {@link PurchaseState} and product ID of the purchase.
		 * In the general case, there can be an array of purchase transactions
		 * because there may be delays in processing the purchase on the backend
		 * and then several purchases can be batched together.
		 * @param signedData the signed JSON string (signed, not encrypted)
		 * @param signature the signature for the data, signed with the private key
		 */
		public static List<VerifiedPurchase> verifyPurchase(string signedData, string signature) {
			if (signedData == null) 
			{
				Log.Error(TAG, "data is null");
				return null;
			}

			if (Consts.DEBUG) 
				Log.Info(TAG, "signedData: " + signedData);
			
			var verified = !ExpectSignature;

			if (!TextUtils.IsEmpty(signature)) 
			{
				/**
				 * Compute your public key (that you got from the Android Market publisher site).
				 *
				 * Instead of just storing the entire literal string here embedded in the
				 * program,  construct the key at runtime from pieces or
				 * use bit manipulation (for example, XOR with some other string) to hide
				 * the actual key.  The key itself is not secret information, but we don't
				 * want to make it easy for an adversary to replace the public key with one
				 * of their own and then fake messages from the server.
				 *
				 * Generally, encryption keys / passwords should only be kept in memory
				 * long enough to perform the operation they need to perform.
				 */
				string base64EncodedPublicKey = "place your key here";
				IPublicKey key = Security.generatePublicKey(base64EncodedPublicKey);
				verified = Security.verify(key, signedData, signature);
				if (!verified) {
					Log.Warn(TAG, "signature does not match data.");
					//return null;
				}
			}

			JsonObject jObject;
			JsonArray jTransactionsArray = null;
			int numTransactions = 0;
			long nonce = 0L;
			try
			{
				jObject = JsonObject.Parse(signedData) as JsonObject;
								
				// The nonce might be null if the user backed out of the buy page.
				nonce = jObject["nonce"];

				jTransactionsArray =  jObject["orders"] as JsonArray;
				if (jTransactionsArray != null)
				{
					numTransactions = jTransactionsArray.Count;
				}
			}
			catch //(MalformedJsonException)
			{
				return null;
			}

			if (!Security.isNonceKnown(nonce))
			{
				Log.Warn(TAG, "Nonce not found: " + nonce);
				return null;
			}

			List<VerifiedPurchase> purchases = new List<VerifiedPurchase>();

			try
			{
				for (int i = 0; i < numTransactions; i++)
				{
					var jElement = jTransactionsArray[i];
					int response = jElement["purchaseState"];
					Consts.PurchaseState purchaseState = (Consts.PurchaseState)response;
					string productId = jElement["productId"];
					string packageName = jElement["packageName"];
					long purchaseTime = jElement["purchaseTime"];
					string orderId = jElement["orderId"];
					
					string notifyId = null;
					string developerPayload = null;

					if (jElement.ContainsKey("notificationId"))
						notifyId = jElement["notificationId"];
					
					if (jElement.ContainsKey("developerPayload"))
						developerPayload = jElement["developerPayload"];

					// If the purchase state is PURCHASED, then we require a
					// verified nonce.
					if (purchaseState == Consts.PurchaseState.PURCHASED && !verified)
						continue;
					
					purchases.Add(new VerifiedPurchase(purchaseState, notifyId, productId,
							orderId, purchaseTime, developerPayload));
				}
			}
			catch (Exception e) //(MalformedJsonException e)
			{
				Log.Error(TAG, "JSON exception: ", e);
				return null;
			}
			removeNonce(nonce);
			return purchases;
		}

		/**
		 * Generates a PublicKey instance from a string containing the
		 * Base64-encoded public key.
		 *
		 * @param encodedPublicKey Base64-encoded public key
		 * @throws IllegalArgumentException if encodedPublicKey is invalid
		 */
		public static IPublicKey generatePublicKey(string encodedPublicKey)
		{
			try {
				byte[] decodedKey = Base64.Decode(encodedPublicKey, 0);
				KeyFactory keyFactory = KeyFactory.GetInstance(KEY_FACTORY_ALGORITHM);
				return keyFactory.GeneratePublic(new X509EncodedKeySpec(decodedKey));
			} catch (NoSuchAlgorithmException e) {
				throw new RuntimeException(e);
			} catch (InvalidKeySpecException e) {
				Log.Error(TAG, "Invalid key specification.");
				throw new IllegalArgumentException(e);
			} catch (Base64DecoderException e) {
				Log.Error(TAG, "Base64 decoding failed.");
				throw new IllegalArgumentException();
			}
		}

		/**
		 * Verifies that the signature from the server matches the computed
		 * signature on the data.  Returns true if the data is correctly signed.
		 *
		 * @param publicKey public key associated with the developer account
		 * @param signedData signed data from server
		 * @param signature server signature
		 * @return true if the data and signature match
		 */
		public static bool verify(IPublicKey publicKey, string signedData, string signature) {
			if (Consts.DEBUG) {
				Log.Info(TAG, "signature: " + signature);
			}
			Signature sig;
			try {
				sig = Signature.GetInstance(SIGNATURE_ALGORITHM);
				sig.InitVerify(publicKey);
				sig.Update(Encoding.UTF8.GetBytes(signedData));
				
				if (!sig.Verify(Base64.Decode(signature, 0))) {
					Log.Error(TAG, "Signature verification failed.");
					return false;
				}
				return true;
			} catch (NoSuchAlgorithmException e) {
				Log.Error(TAG, "NoSuchAlgorithmException.");
			} catch (InvalidKeyException e) {
				Log.Error(TAG, "Invalid key specification.");
			} catch (SignatureException e) {
				Log.Error(TAG, "Signature exception.");
			} catch (Base64DecoderException e) {
				Log.Error(TAG, "Base64 decoding failed.");
			}
			return false;
		}
	}
}