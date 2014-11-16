/// Copyright (C) 2012-2014 Soomla Inc.
///
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///      http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.using System;

using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;

namespace Soomla.Profile
{
	/// <summary>
	/// This is the main class controlling the whole SOOMLA Profile module.
	/// Use this class to perform various social and authentication operations on users.
	/// The Profile module will work with the social and authentication plugins you provide and
	/// define in AndroidManifest.xml or your iOS project's plist.
	/// </summary>
	public class SoomlaProfile
	{
		static SoomlaProfile _instance = null;
		static SoomlaProfile instance {
			get {
				if(_instance == null) {
					#if UNITY_ANDROID && !UNITY_EDITOR
					_instance = new SoomlaProfileAndroid();
					#elif UNITY_IOS && !UNITY_EDITOR
					_instance = new SoomlaProfileIOS();
					#else
					_instance = new SoomlaProfile();
					#endif
				}
				return _instance;
			}
		}

		/// <summary>
		/// The various providers available (currently, only Facebook is available). The functions 
		/// in this class use this <c>providers</c> <c>Dictionary</c> to call the relevant functions 
		/// in each <c>SocialProvider</c> (i.e. Facebook) class.
		/// </summary>
		static Dictionary<Provider, SocialProvider> providers = new Dictionary<Provider, SocialProvider>();

		/// <summary>
		/// Initializes the SOOMLA Profile Module.
		/// 
		/// NOTE: This function must be called before any of the class methods can be used.
		/// </summary>
		public static void Initialize() {
			instance._initialize(GetCustomParamsJson()); //add parameters
#if SOOMLA_FACEBOOK
			providers.Add(Provider.FACEBOOK, new FBSocialProvider());
#endif
#if SOOMLA_GOOGLE
			providers.Add(Provider.GOOGLE, new GPSocialProvider());
#endif
#if SOOMLA_TWITTER
			SoomlaUtils.LogDebug (TAG, "Adding TWITTER provider!!!!!");
			providers.Add(Provider.TWITTER, new TwitterSocialProvider());
#endif

#if UNITY_EDITOR
			ProfileEvents.OnSoomlaProfileInitialized();
#endif
		}

		/// <summary>
		/// Logs the user into the given provider. 
		/// </summary>
		/// <param name="provider">The provider to log in to.</param>
		/// <param name="payload">A string to receive when the function returns.</param>
		/// <param name="reward">A <c>Reward</c> to give the user after a successful login.</param>
		public static void Login(Provider provider, string payload="", Reward reward = null) {
			SoomlaUtils.LogDebug (TAG, "Trying to login with provider " + provider.ToString ());
			SocialProvider targetProvider = GetSocialProvider(provider);
			if (targetProvider == null)
			{
				SoomlaUtils.LogError(TAG, "Provider not supported or not set as active: " + provider.ToString());
				return;
			}

			if (targetProvider.IsNativelyImplemented())
			{
				//fallback to native
				string rewardId = reward != null ? reward.ID : "";
				instance._login(provider, ProfilePayload.ToJSONObj(payload, rewardId).ToString());
			}

			else 
			{
				ProfileEvents.OnLoginStarted(provider, payload);
				targetProvider.Login(
					/* success */	(UserProfile userProfile) => { 
					StoreUserProfile(userProfile);
					ProfileEvents.OnLoginFinished(userProfile, payload); 
					if (reward != null) {
						reward.Give();
					}
				},
				/* fail */		(string message) => {  ProfileEvents.OnLoginFailed (provider, message, payload); },
				/* cancel */	() => {  ProfileEvents.OnLoginCancelled(provider, payload); }
				);
			}
		}

		/// <summary>
		/// Logs the user out of the given provider. 
		/// 
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The provider to log out from.</param>
		public static void Logout(Provider provider) {

			SocialProvider targetProvider = GetSocialProvider(provider);
			if (targetProvider == null)
				return;

			if (targetProvider.IsNativelyImplemented ()) 
			{
				//fallback to native
				instance._logout(provider);

			}

			else
			{
				ProfileEvents.OnLogoutStarted(provider);
				targetProvider.Logout(
					/* success */	() => { ProfileEvents.OnLogoutFinished(provider); },
					/* fail */		(string message) => {  ProfileEvents.OnLogoutFailed (provider, message); }
				);
			}
		}

		/// <summary>
		/// Checks if the user is logged into the given provider.
		/// </summary>
		/// <returns>If is logged into the specified provider, returns <c>true</c>; 
		/// otherwise, <c>false</c>.</returns>
		/// <param name="provider">The provider to check if the user is logged into.</param>
		public static bool IsLoggedIn(Provider provider) {

			SocialProvider targetProvider = GetSocialProvider(provider);
			if (targetProvider == null)
				return false;

			if (targetProvider.IsNativelyImplemented ()) 
			{
				//fallback to native
				return instance._isLoggedIn(provider);
			}

			return targetProvider.IsLoggedIn ();
		}

		/// <summary>
		/// Updates the user's status on the given provider. 
		///
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The <c>Provider</c> the given status should be posted to.</param>
		/// <param name="status">The actual status text.</param>
		/// <param name="payload">A string to receive when the function returns.</param>
		/// <param name="reward">A <c>Reward</c> to give the user after a successful post.</param>
		public static void UpdateStatus(Provider provider, string status, string payload="", Reward reward = null) {

			SocialProvider targetProvider = GetSocialProvider(provider);

			if (targetProvider == null)
				return;

			if (targetProvider.IsNativelyImplemented())
			{
				//fallback to native
				SoomlaUtils.LogDebug(TAG, "DIMA: Update status with payload = " + payload);
				string rewardId = reward != null ? reward.ID : "";
				instance._updateStatus(provider, status, ProfilePayload.ToJSONObj(payload, rewardId).ToString());
			}

			else 
			{
				ProfileEvents.OnSocialActionStarted(provider, SocialActionType.UPDATE_STATUS, payload);
				targetProvider.UpdateStatus(status,
				    /* success */	() => {
					ProfileEvents.OnSocialActionFinished(provider, SocialActionType.UPDATE_STATUS, payload); 
					if (reward != null) {
						reward.Give();
					}
				},
					/* fail */		(string error) => {  ProfileEvents.OnSocialActionFailed (provider, SocialActionType.UPDATE_STATUS, error, payload); }
				);
			}
		}

		/// <summary>
		/// Posts a full story to the user's social page on the given Provider. 
		/// A story contains a title, description, image and more.
		///
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The <c>Provider</c> the given story should be posted to.</param>
		/// <param name="message">A message that will be shown along with the story.</param>
		/// <param name="name">The name (title) of the story.</param>
		/// <param name="caption">A caption.</param>
		/// <param name="description">A description.</param>
		/// <param name="link">A link to a web page.</param>
		/// <param name="pictureUrl">A link to an image on the web.</param>
		/// <param name="payload">A string to receive when the function returns.</param>
		/// <param name="reward">A <c>Reward</c> to give the user after a successful post.</param>
		public static void UpdateStory(Provider provider, string message, string name,
		                               string caption, string description, string link, string pictureUrl, 
		                               string payload="", Reward reward = null) {

			SocialProvider targetProvider = GetSocialProvider(provider);
			if (targetProvider == null)
				return;

			if (targetProvider.IsNativelyImplemented())
			{
				//fallback to native
				string rewardId = reward != null ? reward.ID: "";
				instance._updateStory(provider, message, name, caption, description, link, pictureUrl, 
				                      ProfilePayload.ToJSONObj(payload, rewardId).ToString());
			}

			else
			{
				ProfileEvents.OnSocialActionStarted(provider, SocialActionType.UPDATE_STORY, payload);
				targetProvider.UpdateStory(message, name, caption, link, pictureUrl,
				    /* success */	() => { 
					ProfileEvents.OnSocialActionFinished(provider, SocialActionType.UPDATE_STORY, payload); 
					if (reward != null) {
						reward.Give();
					}
				},
					/* fail */		(string error) => {  ProfileEvents.OnSocialActionFailed (provider, SocialActionType.UPDATE_STORY, error, payload); },
					/* cancel */	() => {  ProfileEvents.OnSocialActionCancelled(provider, SocialActionType.UPDATE_STORY, payload); }
				);
			}
		}

//		public static void UploadImage(Provider provider, string message, string filename,
//		                               byte[] imageBytes, int quality, Reward reward) {
//			instance._uploadImage(provider, message, filename, imageBytes, quality, reward);
//		}
//

		/// <summary>
		/// Uploads an image to the user's social page on the given Provider.
		/// 
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The <c>Provider</c> the given image should be uploaded to.</param>
		/// <param name="tex2D">Texture2D for image.</param>
		/// <param name="fileName">Name of image file.</param>
		/// <param name="message">Message to post with the image.</param>
		/// <param name="payload">A string to receive when the function returns.</param>
		/// <param name="reward">A <c>Reward</c> to give the user after a successful upload.</param>
		public static void UploadImage(Provider provider, Texture2D tex2D, string fileName, string message, string payload="",
		                               Reward reward = null) {

			SocialProvider targetProvider = GetSocialProvider(provider);

			if (targetProvider == null)
				return;

			if (targetProvider.IsNativelyImplemented())
			{
				//fallback to native
				ProfileEvents.OnSocialActionFailed(provider, 
				                                   SocialActionType.UPLOAD_IMAGE, 
				                                   "Image uploading is not supported with Texture for natively implemented social providers",
				                                   payload);
			}

			else 
			{
				ProfileEvents.OnSocialActionStarted(provider, SocialActionType.UPLOAD_IMAGE, payload);
				targetProvider.UploadImage(tex2D.EncodeToPNG(), fileName, message,
				    /* success */	() => { 
					ProfileEvents.OnSocialActionFinished(provider, SocialActionType.UPLOAD_IMAGE, payload); 
					if (reward != null) {
						reward.Give();
					}
				},
				/* fail */		(string error) => {  ProfileEvents.OnSocialActionFailed (provider, SocialActionType.UPLOAD_IMAGE, error, payload); },
				/* cancel */	() => {  ProfileEvents.OnSocialActionCancelled(provider, SocialActionType.UPLOAD_IMAGE, payload); }
				);
			}
		}

		// <summary>
		// Uploads an image to the user's social page on the given Provider.
		// 
		// NOTE: This operation requires a successful login.
		// </summary>
		// <param name="provider">The <c>Provider</c> the given image should be uploaded to.</param>
		// <param name="message">Message to post with the image.</param>
		// <param name="filePath">Path of image file.</param>
		// <param name="payload">A string to receive when the function returns.</param>
		// <param name="reward">A <c>Reward</c> to give the user after a successful upload.</param>
		public static void UploadImage(Provider provider, string message, string filePath, string payload="",
		                               Reward reward = null) {
			
			SocialProvider targetProvider = GetSocialProvider(provider);
			
			if (targetProvider == null)
				return;
			
			if (targetProvider.IsNativelyImplemented())
			{
				//fallback to native
				string rewardId = reward != null ? reward.ID : "";
				instance._uploadImage(provider, message, filePath, ProfilePayload.ToJSONObj(payload, rewardId).ToString());
			}
			
			else 
			{
				Texture2D tex2D = new Texture2D(4, 4);
				tex2D.LoadImage(File.ReadAllBytes(filePath));
				string fileName = Path.GetFileName(filePath);

				UploadImage(provider, tex2D, fileName, message, payload, reward);
			}
		}

		/// <summary>
		/// Uploads the current screen shot image to the user's social page on the given Provider.
		/// 
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <param name="mb">Mb.</param>
		/// <param name="provider">The <c>Provider</c> the given screenshot should be uploaded to.</param>
		/// <param name="title">The title of the screenshot.</param>
		/// <param name="message">Message to post with the screenshot.</param>
		/// <param name="payload">A string to receive when the function returns.</param>
		/// <param name="reward">A <c>Reward</c> to give the user after a successful upload.</param>
		public static void UploadCurrentScreenShot(MonoBehaviour mb, Provider provider, string title, string message, string payload="", Reward reward = null) {
			mb.StartCoroutine(TakeScreenshot(provider, title, message, payload, reward));
		}

		/// <summary>
		/// Fetches UserProfiles of contacts of the current user.
		///
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The <c>Provider</c> to fetch contacts from.</param>
		/// <param name="payload">A string to receive when the function returns.</param>
		public static void GetContacts(Provider provider, string payload="") {

			SocialProvider targetProvider = GetSocialProvider(provider);

			if (targetProvider == null)
				return;

			if (targetProvider.IsNativelyImplemented())
			{
				//fallback to native
				instance._getContacts(provider, ProfilePayload.ToJSONObj(payload).ToString());
			}

			else 
			{
				ProfileEvents.OnGetContactsStarted(provider, payload);
				targetProvider.GetContacts(
					/* success */	(List<UserProfile> profiles) => { 
					ProfileEvents.OnGetContactsFinished(provider, profiles, payload);
				},
				/* fail */		(string message) => {  ProfileEvents.OnGetContactsFailed(provider, message, payload); }
				);
			}
		}

		// TODO: this is irrelevant for now. Will be updated soon.
//		public static void AddAppRequest(Provider provider, string message, string[] to, string extraData, string dialogTitle) {
//			providers[provider].AppRequest(message, to, extraData, dialogTitle,
//			    /* success */	(string requestId, List<string> recipients) => {
//									string requestsStr = KeyValueStorage.GetValue("soomla.profile.apprequests");
//									List<string> requests = new List<string>();
//									if (!string.IsNullOrEmpty(requestsStr)) {
//										requests = requestsStr.Split(',').ToList();
//									}
//									requests.Add(requestId);
//									KeyValueStorage.SetValue("soomla.profile.apprequests", string.Join(",", requests.ToArray()));
//									KeyValueStorage.SetValue(requestId, string.Join(",", recipients.ToArray()));
//									ProfileEvents.OnAddAppRequestFinished(provider, requestId);
//								},
//				/* fail */		(string errMsg) => {
//									ProfileEvents.OnAddAppRequestFailed(provider, errMsg);
//								});
//		}


		/// <summary>
		///  Will fetch posts from user feed
		///
		/// </summary>
		/// <param name="provider">Provider.</param>
		/// <param name="reward">Reward.</param>
//		public static void GetFeed(Provider provider, Reward reward) {
//
//			// TODO: implement with FB SDK
//
//		}

		/// <summary>
		/// Likes the page (with the given name) of the given provider.
		/// 
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The provider that the page belongs to.</param>
		/// <param name="pageName">The name of the page to like.</param>
		/// <param name="reward">A <c>Reward</c> to give the user after he/she likes the page.</param>
		public static void Like(Provider provider, string pageName, Reward reward=null) {
			SocialProvider targetProvider = GetSocialProvider(provider);
			if (targetProvider != null) {
				targetProvider.Like(pageName);

				if (reward != null) {
					reward.Give();
				}
			}
		}
	
		/// <summary>
		/// Fetches the saved user profile for the given provider. UserProfiles are automatically 
		/// saved in the local storage for a provider after a successful login.
		/// 
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <returns>The stored user profile.</returns>
		/// <param name="provider">The provider to fetch UserProfile from.</param>
		public static UserProfile GetStoredUserProfile(Provider provider) {
			return instance._getStoredUserProfile(provider);
		}

		/// <summary>
		/// Stores the given user profile in the relevant provider (contained internally in the UserProfile).
		/// 
		/// NOTE: This operation requires a successful login.
		/// </summary>
		/// <param name="userProfile">User profile to store.</param>
		/// <param name="notify">If set to <c>true</c>, notify.</param>
		public static void StoreUserProfile (UserProfile userProfile, bool notify = false) {
			instance._storeUserProfile (userProfile, notify);
		}

		/// <summary>
		/// Opens the app rating page.
		/// 
		/// NOTE: This operation requires a successful login.
		/// </summary>
		public static void OpenAppRatingPage() {
			instance._openAppRatingPage ();

			ProfileEvents.OnUserRatingEvent ();
		}

		public static bool IsProviderNativelyImplemented(Provider provider) {
			SocialProvider targetProvider = GetSocialProvider(provider);
			if (targetProvider != null) {
				return targetProvider.IsNativelyImplemented();
			}

			return false;
		}

		private static SocialProvider GetSocialProvider (Provider provider)
		{
			SocialProvider result = null;
			providers.TryGetValue(provider, out result);

//			if (result == null) {
//				throw new ProviderNotFoundException();
//			}

			return result;
		}

		private static string GetCustomParamsJson()
		{
			Dictionary<string, string> gpParams = new Dictionary<string, string>()
			{
				{"clientId", ProfileSettings.GPClientId}
			};

			Dictionary<string, string> twParams = new Dictionary<string, string> ()
			{
				{"consumerKey", ProfileSettings.TwitterConsumerKey},
				{"consumerSecret", ProfileSettings.TwitterConsumerSecret}
			};

			Dictionary<Provider, Dictionary<string, string>> customParams =  new Dictionary<Provider, Dictionary<string, string>> ()
			{
				{Provider.GOOGLE, gpParams},
				{Provider.TWITTER, twParams}
			};

			JSONObject customParamsJson = JSONObject.Create();
			foreach(KeyValuePair<Provider, Dictionary<string, string>> parameter in customParams)
			{
				string currentProvider = parameter.Key.ToString();
				JSONObject currentProviderParams = new JSONObject(parameter.Value);
				customParamsJson.AddField(currentProvider, currentProviderParams);
			}

			return customParamsJson.ToString ();
		}

		/** PROTECTED & PRIVATE FUNCTIONS **/

		protected virtual void _initialize(string customParamsJson) { }

		protected virtual void _login(Provider provider, string payload) { }

		protected virtual void _logout (Provider provider) { }

		protected virtual bool _isLoggedIn(Provider provider) { return false; }

		protected virtual void _updateStatus(Provider provider, string status, string payload) { }

		protected virtual void _updateStory (Provider provider, string message, string name,
		                                     string caption, string description, string link,
		                                     string pictureUrl, string payload) { }

		protected virtual void _uploadImage(Provider provider, string message, string filePath, string payload) { }

		protected virtual void _getContacts(Provider provider, string payload) { }

		protected virtual void _openAppRatingPage() { }

		protected virtual UserProfile _getStoredUserProfile(Provider provider) {
#if UNITY_EDITOR
			string key = keyUserProfile(provider);
			string value = PlayerPrefs.GetString (key);
			if (!string.IsNullOrEmpty(value)) {
				return new UserProfile (new JSONObject (value));
			}
#endif
			return null;
		}

		protected virtual void _storeUserProfile(UserProfile userProfile, bool notify) {
#if UNITY_EDITOR
			string key = keyUserProfile(userProfile.Provider);
			string val = userProfile.toJSONObject().ToString();
			SoomlaUtils.LogDebug(TAG, "key/val:" + key + "/" + val);
			PlayerPrefs.SetString(key, val);

			if (notify) {
				ProfileEvents.OnUserProfileUpdated(userProfile);
			}
#endif
		}

		private static IEnumerator TakeScreenshot(Provider provider, string title, string message, string payload, Reward reward)
		{
			yield return new WaitForEndOfFrame();
			
			var width = Screen.width;
			var height = Screen.height;
			var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
			// Read screen contents into the texture
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();
			
			UploadImage(provider, tex, title, message, payload, reward);
		}

		/** keys when running in editor **/
#if UNITY_EDITOR
		private const string DB_KEY_PREFIX = "soomla.profile.";

		private static string keyUserProfile(Provider provider) {
			return DB_KEY_PREFIX + "userprofile." + provider.ToString();
		}
#endif
		/** CLASS MEMBERS **/

		protected const string TAG = "SOOMLA SoomlaProfile";
	}
}
