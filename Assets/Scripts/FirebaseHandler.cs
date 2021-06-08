using System;
using System.Collections;
using System.Collections.Generic;
using Facebook.Unity;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

public class FirebaseHandler : MonoBehaviour
{
    public static event Action<bool> SignInAction = delegate { };
    public static event Action<object, EventArgs> AuthStateChanged = delegate { };

    private static FirebaseHandler instance = null;

    public static FirebaseHandler Instance
    {
        get
        {
            if (instance != null) return instance;

            var go = new GameObject {name = nameof(FirebaseHandler)};
            DontDestroyOnLoad(go);
            instance = go.AddComponent<FirebaseHandler>();
            return instance;
        }
    }

    public bool IsInitialized => IsInitialized;
    public FirebaseApp App => App;
    public FirebaseAuth Auth => auth;

    private FirebaseApp app;
    private FirebaseAuth auth;
    private bool isInitialized = false;

    public async void Initialized()
    {
        var result = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (result == DependencyStatus.Available)
        {
            app = FirebaseApp.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            auth.StateChanged += AuthOnStateChanged;
            FB.Init(InitCallBack, OnHideUnity);
        }
        else
        {
            Debug.LogError($"Could not resolve all Firebase Dependencies: {result}");
        }
    }

    private void InitCallBack()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
            isInitialized = true;
            Debug.Log("Firebase and Facebook initialized");
        }
        else
        {
            Debug.LogWarning("Failed to initialize Facebook SDK");
        }
    }

    private void OnHideUnity(bool isUnityShown)
    {
        if (isUnityShown) FB.ActivateApp();
    }

    private static void AuthOnStateChanged(object sender, EventArgs e)
    {
        AuthOnStateChanged(sender, e);
    }

    public void SignInFacebook()
    {
        var perms = new List<string>() {"public_profile", "email"};
        FB.LogInWithReadPermissions(perms, OnFacebookLoginResult);
    }

    private void OnFacebookLoginResult(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            var accessToken = AccessToken.CurrentAccessToken;
            SignInFirebase(accessToken);
        }
        else
        {
            SignInAction(false);
            Debug.Log("User cancel login");
        }
    }

    private void SignInFirebase(AccessToken accessToken)
    {
        var credential = FacebookAuthProvider.GetCredential(accessToken.TokenString);
        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                //TODO Handle sign in cancel
                Debug.Log("Sign in cancelled");
                SignInAction(false);
                return;
            }

            if (task.IsFaulted)
            {
                //TODO handle sign in fault
                Debug.Log("Sign in error: " + task.Exception);
                SignInAction(false);
                return;
            }

            FirebaseUser newUser = task.Result;
            Debug.Log($"User signed in: {newUser.DisplayName}, {newUser.UserId}");
            SignInAction(true);
        });
    }

    public void SignIn()
    {
        SignInFacebook();
    }
}
