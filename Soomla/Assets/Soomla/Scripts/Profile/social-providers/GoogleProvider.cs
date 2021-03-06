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
/// limitations under the License.
using System;
using UnityEngine;
using System.Collections.Generic;

namespace Soomla.Profile
{
	public class GoogleProvider : IAuthProvider, ISocialProvider, IGameServicesProvider
	{
		private static string TAG = "SOOMLA GoogleProvider";

		public GoogleProvider () {
			SoomlaProfile.ProviderBecameReady(this);
		}

		/// <summary>
		/// See docs in <see cref="SoomlaProfile.UpdateStatus"/>
		/// </summary>
		public void UpdateStatus(string status, SocialActionSuccess success, SocialActionFailed fail) {}

		/// <summary>
		/// See docs in <see cref="SoomlaProfile.UpdateStatusDialog"/>
		/// </summary>
		public void UpdateStatusDialog(string link, SocialActionSuccess success, SocialActionFailed fail) {}

		/// <summary>
		/// See docs in <see cref="SoomlaProfile.UpdateStory"/>
		/// </summary>
		public void UpdateStory(string message, string name, string caption, string description,
		                        string link, string pictureUrl, SocialActionSuccess success, SocialActionFailed fail, SocialActionCancel cancel) {}

		/// <summary>
		/// See docs in <see cref="SoomlaProfile.UpdateStoryDialog"/>
		/// </summary>
		public void UpdateStoryDialog(string name, string caption, string description, string link, string picture,
		                                       SocialActionSuccess success, SocialActionFailed fail, SocialActionCancel cancel) {}
		/// <summary>
		/// See docs in <see cref="SoomlaProfile.UploadImage"/>
		/// </summary>
		public void UploadImage(byte[] texBytes, string fileName, string message, SocialActionSuccess success, SocialActionFailed fail, SocialActionCancel cancel) {}
		
		/// <summary>
		/// See docs in <see cref="SoomlaProfile.GetContacts"/>
		/// </summary>
		public void GetContacts(bool fromStart, ContactsSuccess success, ContactsFailed fail) {}

		/// <summary>
		/// See docs in <see cref="SoomlaProfile.GetFeed"/>
		/// </summary>
		public void GetFeed(bool fromStart, FeedSuccess success, FeedFailed fail) {}
		
		/// <summary>
		/// See docs in <see cref="SoomlaProfile.Logout"/>
		/// </summary>
		public void Logout(LogoutSuccess success, LogoutFailed fail) {}
		
		/// <summary>
		/// See docs in <see cref="SoomlaProfile.Login"/>
		/// </summary>
		public void Login(LoginSuccess success, LoginFailed fail, LoginCancelled cancel) {}

		/// <summary>
		/// See docs in <see cref="SoomlaProfile.GetUserProfile"/>
		/// </summary>
		public void GetUserProfile(GetUserProfileSuccess success, GetUserProfileFailed fail) {
		}
		
		/// <summary>
		/// See docs in <see cref="SoomlaProfile.IsLoggedIn"/>
		/// </summary>
		public bool IsLoggedIn() {
			return false;
		}

		/// <summary>
		/// See docs in <see cref="ISocialProvider.IsAutoLogin"/>
		/// </summary>
		/// <returns>value of autoLogin
		public bool IsAutoLogin() {
			return false;
		}
		
		/// <summary>
		/// See docs in <see cref="SoomlaProfile.Invite"/>
		/// </summary>
		public void Invite (string inviteMessage, string dialogTitle, InviteSuccess success, InviteFailed fail, InviteCancelled cancel) {}
		
		/// <summary>
		/// See docs in <see cref="SoomlaProfile.AppRequest"/>
		/// </summary>
		public void AppRequest(string message, string[] to, string extraData, string dialogTitle, AppRequestSuccess success, AppRequestFailed fail) {}
		
		/// <summary>
		/// See docs in <see cref="SoomlaProfile.Like"/>
		/// </summary>
		public void Like(string pageId) {
			SoomlaUtils.LogDebug (TAG, "Like");
			Application.OpenURL("https://plus.google.com/+" + pageId);
		}

		public void GetLeaderboards(SocialPageDataSuccess<Leaderboard> success, FailureHandler fail) {}

		public void GetScores(Leaderboard owner, bool fromStart, SocialPageDataSuccess<Score> success, FailureHandler fail) {}

		public void SubmitScore(Leaderboard targetLeaderboard, int value, SingleObjectSuccess<Score> success, FailureHandler fail) {}

		public void ShowLeaderboards() {}

		public void Configure(Dictionary<string, string> providerParams) { }

		public bool IsNativelyImplemented(){
			return true;
		}
	}
}


