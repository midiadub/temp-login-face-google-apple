using System;
using Balaso;
using Midiadub.Analytics;
using Midiadub.Authentication;
using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginTest : MonoBehaviour
{
    public string webClientId;
    public string currentUser;
    public TextMeshProUGUI displayUser;
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

    public GameObject[] buttons;

    private void Awake()
    {
        _firebaseAnalytics = new FirebaseAnalyticRepository();
        _firebaseAnalytics.SetAuthorization(AppTrackingTransparency.AuthorizationStatus.AUTHORIZED);
        _auth = new LoginManager(_firebaseAnalytics, webClientId);
    }

    void Start()
    {
        
    }

    private void Update()
    {
        _auth.DisplayCurrentUser();

        if (_auth.currentUser != null)
        {
            currentUser = _auth.currentUser;
            displayUser.text = currentUser;
        }
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
    
    public void LoginAnon()
    {
        _auth.LoginAnon();
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

    public void Logout()
    {
        _auth.SignOut();
    }

    public void DisableAllButtons()
    {
        foreach (GameObject button in buttons)
        {
            button.SetActive(false);
        }
    }
}
