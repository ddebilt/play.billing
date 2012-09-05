using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace play.billing
{
	[Activity(Label = "play.billing", MainLauncher = true, Icon = "@drawable/icon")]
	public class App : Activity
	{
		BillingService m_service;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);

			//Change this to true if you expect signed responses 
			//  see: http://developer.android.com/guide/google/play/billing/billing_testing.html
			//You may also need to deploy a signed package to test with as well. I was only able to get signed data with a signed package when testing
			//  see: http://docs.xamarin.com/android/tutorials/Preparing_Package_for_Android_Marketplace
			Security.ExpectSignature = false; 
			
			ResponseHandler.register(new PurchaseObserver(this));

			m_service = new BillingService();
			m_service.setContext(this);

			var pButton = FindViewById<Button>(Resource.Id.PurchaseButton);
			pButton.Click += delegate { m_service.RequestPurchase("android.test.purchased"); };

			var cButton = FindViewById<Button>(Resource.Id.CancelButton);
			cButton.Click += delegate { m_service.RequestPurchase("android.test.canceled"); };

			var rButton = FindViewById<Button>(Resource.Id.RefundButton);
			rButton.Click += delegate { m_service.RequestPurchase("android.test.refunded"); };

			var uButton = FindViewById<Button>(Resource.Id.UnavailableButton);
			uButton.Click += delegate { m_service.RequestPurchase("android.test.item_unavailable"); };
		}
	}
}

