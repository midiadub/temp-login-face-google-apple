using UnityEngine;
using System.Collections;
using TwitterKit.Unity;
using TMPro;
using Firebase.Auth;

public class TwitterDemo : MonoBehaviour
{
	public TextMeshProUGUI userId;
	
	private FirebaseAuth auth;
	
	void Awake()
	{
		auth = FirebaseAuth.DefaultInstance;
	}
	
	void Start ()
	{
	}

	public void startLogin() {
		UnityEngine.Debug.Log ("startLogin()");
		// To set API key navigate to tools->Twitter Kit
		Twitter.Init ();
		
		Twitter.LogIn (Teste, (ApiError error) => {
			UnityEngine.Debug.Log (error.message);
		});
	}
	/*
	public void LoginCompleteWithEmail (TwitterSession session) {
		// To get the user's email address you must have "Request email addresses from users" enabled on https://apps.twitter.com/ (Permissions -> Additional Permissions)
		UnityEngine.Debug.Log ("LoginCompleteWithEmail()");
		Twitter.RequestEmail (session, RequestEmailComplete, (ApiError error) => { UnityEngine.Debug.Log (error.message); });
	}
	
	public void RequestEmailComplete (string email) {
		UnityEngine.Debug.Log ("email=" + email);
		LoginCompleteWithCompose ( Twitter.Session );
	}
	
	public void LoginCompleteWithCompose(TwitterSession session) {
		ScreenCapture.CaptureScreenshot("Screenshot.png");
		UnityEngine.Debug.Log ("Screenshot location=" + Application.persistentDataPath + "/Screenshot.png");
		string imageUri = "file://" + Application.persistentDataPath + "/Screenshot.png";
		Twitter.Compose (session, imageUri, "Welcome to", new string[]{"#TwitterKitUnity"},
			(string tweetId) => { UnityEngine.Debug.Log ("Tweet Success, tweetId=" + tweetId); },
			(ApiError error) => { UnityEngine.Debug.Log ("Tweet Failed " + error.message); },
			() => { Debug.Log ("Compose cancelled"); }
		 );
		
		Teste( Twitter.Session );
	}*/

	public void Teste(TwitterSession session)
	{
		Firebase.Auth.Credential credential =
		Firebase.Auth.TwitterAuthProvider.GetCredential(session.authToken.token, session.authToken.secret);
		auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
		{
			if (task.IsCanceled)
			{
				Debug.LogError("SignInWithCredentialAsync was canceled.");
				return;
			}
			if (task.IsFaulted)
			{
				Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
				return;
			}

			Firebase.Auth.FirebaseUser newUser = task.Result;
			Debug.LogFormat("User signed in successfully: {0} ({1})",
				newUser.DisplayName, newUser.UserId);
		});

		
		userId.text = session.userName + " || " + session.userName + " || " + session.authToken.secret + " || " + session.authToken.token;
	}
}
