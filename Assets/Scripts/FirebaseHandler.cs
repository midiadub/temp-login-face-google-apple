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
    //public static event Action<object, EventArgs> AuthStateChanged = delegate { };

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
            //auth.StateChanged += AuthOnStateChanged;
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

/*  private static void AuthOnStateChanged(object sender, EventArgs e)
    {
        AuthOnStateChanged(sender, e);
    }    */

    public void SignInFacebook()
    {
        var perms = new List<string>() {"public_profile", "email"};                             //Cria uma lista de permissões que o Facebook libera
        FB.LogInWithReadPermissions(perms, OnFacebookLoginResult);                              //Executa comando do SDK para solicitar ao usuário acesso às informações declaradas em perms. Se o usuário já permitiu antes, o comando apenas verifica a confirmação
    }

    private void OnFacebookLoginResult(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            var accessToken = AccessToken.CurrentAccessToken;                                   //Solicita o AccessToken do usuário
            SignInFirebase(accessToken);                                                        //Utiliza o Accesstoken recebido para solicitar ao banco informações únicas
        }
        else
        {
            SignInAction(false);
            Debug.Log("User cancel login");
        }
    }

    private void SignInFirebase(AccessToken accessToken)
    {
        var credential = FacebookAuthProvider.GetCredential(accessToken.TokenString);          //Preenche as credenciais do usuário utilizando o accessToken como referência
        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>          //Efetua o login no Firebase usando a credencial/acesstoken como referência
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
