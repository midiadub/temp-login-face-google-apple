using System;
using Balaso;
using Midiadub.Analytics;
using Midiadub.Authentication;
using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.UI;

public class LoginTest : MonoBehaviour
{
    public string webClientId;
    private LoginManager _auth;
    private FirebaseAnalyticRepository _firebaseAnalytics;

    //LoginComEmail
    public InputField emailLogin;
    public InputField passwordLogin;
    
    //CadastroComEmail
    public InputField emailCreate;
    public InputField passwordCreate;
    //public InputField passwordConfirmation;
    public InputField username;

    private void Awake()
    {
        _firebaseAnalytics = new FirebaseAnalyticRepository();
        _firebaseAnalytics.SetAuthorization(AppTrackingTransparency.AuthorizationStatus.AUTHORIZED);
        _auth = new LoginManager(_firebaseAnalytics, webClientId);
    }

    void Start()
    {
        
    }

    public void LoginGoogle()
    {
        _auth.OnGoogleSignIn();
    }

    public void LoginFacebook()
    {
        _auth.SignInFacebook();
    }
    
    public void LoginTwitter()
    {
        _auth.LoginTwitter();
    }
    
    public void LoginEmail()
    {
        StartCoroutine(_auth.LoginWithEmail(emailLogin.text, passwordLogin.text));
        Debug.Log(emailLogin.text + " " + passwordLogin.text);
    }

    public void CadastroEmail()
    {
        StartCoroutine(_auth.RegisterWithEmail(emailCreate.text, passwordCreate.text, username.text));
        Debug.Log(emailCreate.text + " " + passwordCreate.text);
    }
    
}
