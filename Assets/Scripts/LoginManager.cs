using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private Button signInButton;

    private void Awake()
    {
        FirebaseHandler.Instance.Initialized();                                     //responsável por inicializar o singleton do FirebaseHandler
    }

    public async void Start()
    {
        FirebaseHandler.SignInAction += FirebaseHandlerOnSignInAction;              //Chama o método pra exibir que o usuário foi logado após o Firebase confirmar
        
        signInButton.onClick.AddListener(OnSignInClick);                            //Adiciona o método OnSignInclick para executar ao tocar no botão de Sign In
    }

    private void FirebaseHandlerOnSignInAction(bool signedIn)
    {
        if (signedIn)
        {
            //TODO Após login trocar o botão para SignOut
            Debug.Log("Signed in");
        }
    }

    private void OnSignInClick()
    {
        FirebaseHandler.Instance.SignIn();                                         //Ao tocar no botão de Sign In é chamado o método SignIn do singleton do FirebaseHandler
    }
}
