using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    //[SerializeField] private Button playButton;
    //[SerializeField] private Button menuButton;
    [SerializeField] private Button signInButton;

    private void Awake()
    {
        FirebaseHandler.Instance.Initialized();
    }

    public async void Start()
    {
        //Event Listeners
        FirebaseHandler.SignInAction += FirebaseHandlerOnSignInAction;
        
        //Button listeners
        //playButton.onClick.AddListener(OnPlayClick);
        //menuButton.onClick.AddListener(Restart);
        signInButton.onClick.AddListener(OnSignInClick);

        //ResetGame();
        //await ShowSplash();
    }

    private void FirebaseHandlerOnSignInAction(bool signedIn)
    {
        if (signedIn)
        {
            //TODO Handle sign in UI
            Debug.Log("Signed in");
        }
    }

    private void OnSignInClick()
    {
        FirebaseHandler.Instance.SignIn();
    }

    private void OnPlayClick()
    {
        //
    }

    private void Restart()
    {
        //
    }


}
