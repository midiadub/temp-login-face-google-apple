using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Facebook.Unity;
using Google;
using Midiadub.Analytics;
using TwitterKit.Unity;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Firebase.Extensions;

namespace Midiadub.Authentication
{
    public class LoginManager
    {
        //Firebase variables
        DependencyStatus dependencyStatus;
        FirebaseAuth auth;
        FirebaseUser user;
        private FirebaseApp app;
        private readonly IAnalyticsRepository analytics;
        private readonly string webClientId;
        private bool isInitialized;
        public static event Action<bool> SignInAction = delegate { };
        private GoogleSignInConfiguration configuration;
        
        private IAppleAuthManager appleAuthManager;
        
        //Sistema de eventos para erros
        public enum LoginReturn
        {
            none,
            twitter,
            facebook,
            emailLogin,
            emailCadastro,
            gmail,
            anom,
            apple
        }
        public LoginReturn typeMessage;
        
        public delegate void ErrorAction(LoginReturn _typeMessage);
        public static event ErrorAction LoginError;
        
        //Sistema de eventos para Login/Logout
        public delegate void LogInAction();
        public static event LogInAction SignedIn;
        public static event LogInAction SignedOut;
        
        //
        public string currentUser;

        public LoginManager(IAnalyticsRepository analytics, string webClientId)
        {
            this.analytics = analytics;
            this.webClientId = webClientId;
            if (this.analytics.IsInitialized(InitializeFirebase))
            {
                InitializeFirebase();
            }
        }

        private void InitializeFirebase()
        {
            //Set the authentication instance object
            auth = FirebaseAuth.DefaultInstance;
            
            //Iniciando gerenciador de usuário Logado/Deslogado
            auth.StateChanged += AuthStateChanged;
            AuthStateChanged(this, null);

            //Inicializando Google
            configuration = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true
            };
            
            #if DEV_MODE
            Debug.Log("Iniciou Google");
            #endif

            InitializeApple();
            InitializeFacebook();
        }



        #region Email

        public IEnumerator LoginWithEmail(string _email, string _password)
        {
            //Debug.Log("tried login");
            //Call the Firebase auth signin function passing the email and password
            var LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
            //Wait until the task completes
            yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

            if (LoginTask.Exception != null)
            {
                //If there are errors handle them
                Debug.LogWarning(message: $"Failed to register task with {LoginTask.Exception}");
                FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError) firebaseEx.ErrorCode;

                string message = "Login Failed!";
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WrongPassword:
                        message = "Wrong Password";
                        break;
                    case AuthError.InvalidEmail:
                        message = "Invalid Email";
                        break;
                    case AuthError.UserNotFound:
                        message = "Account does not exist";
                        break;
                }
#if DEV_MODE
                Debug.Log(message);
#endif
            }
            else
            {
                //User is now logged in
                //Now get the result
                user = LoginTask.Result;
#if DEV_MODE
            Debug.LogFormat("User signed in successfully: {0} ({1})", user.DisplayName, user.Email);
#endif
            }
        }

        public IEnumerator RegisterWithEmail(string _email, string _password, string _username)
        {
            if (_username == "")
            {
                
                //If the username field is blank show a warning
            }
            else
            {
                //Call the Firebase auth signin function passing the email and password
                var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);
                //Wait until the task completes
                yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

                if (RegisterTask.Exception != null)
                {
#if DEV_MODE
            Debug.LogError(message: $"Failed to register task with {RegisterTask.Exception}");
#endif
                }
                else
                {
                    user = RegisterTask.Result;

                    if (user != null)
                    {
                        //Create a user profile and set the username
                        UserProfile profile = new UserProfile {DisplayName = _username};

                        //Call the Firebase auth update user profile function passing the profile with the username
                        var ProfileTask = user.UpdateUserProfileAsync(profile);
                        //Wait until the task completes
                        yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                        if (ProfileTask.Exception != null)
                        {
#if DEV_MODE
                        Debug.LogError(message: $"Failed to register task with {ProfileTask.Exception}");
#endif
                        }
                    }
                }
            }
        }

        #endregion

        #region Facebook

        private void InitializeFacebook()
        {
            app = FirebaseApp.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            //auth.StateChanged += AuthOnStateChanged;
            #if DEV_MODE
            Debug.Log("Iniciou Facebook");
            #endif
            FB.Init(InitFacebookCallBack, OnFacebookPopup);
        }
        
        private void InitFacebookCallBack()
        {
            if (FB.IsInitialized)
            {
                FB.ActivateApp();
                isInitialized = true;
                #if DEV_MODE
                Debug.Log("Firebase and Facebook initialized");
                #endif
            }
            else
            {
                #if DEV_MODE
                Debug.LogWarning("Failed to initialize Facebook SDK");
                #endif
            }
        }

        private void OnFacebookPopup(bool isUnityShown)
        {
            if (isUnityShown) FB.ActivateApp();
        }

        public void SignInFacebook()
        {
            if (FB.IsLoggedIn)
                FB.LogOut();
            
            var perms = new List<string>()
                {"email"}; //Cria uma lista de permissões que o Facebook libera
            FB.LogInWithReadPermissions(perms,
                OnFacebookLoginResult); //Executa comando do SDK para solicitar ao usuário acesso às informações declaradas em perms. Se o usuário já permitiu antes, o comando apenas verifica a confirmação
        }

        private void OnFacebookLoginResult(ILoginResult result)
        {
            if (FB.IsLoggedIn)
            {
                var accessToken = AccessToken.CurrentAccessToken; //Solicita o AccessToken do usuário
                SignInFirebase(accessToken); //Utiliza o Accesstoken recebido para solicitar ao banco informações únicas
            }
            else
            {
                SignInAction(false);
                #if DEV_MODE
                Debug.Log("User cancel login");
                #endif
            }
        }

        private void SignInFirebase(AccessToken accessToken)
        {
            var credential =
                FacebookAuthProvider.GetCredential(accessToken
                    .TokenString); //Preenche as credenciais do usuário utilizando o accessToken como referência
            auth.SignInWithCredentialAsync(credential).ContinueWith(
                task => //Efetua o login no Firebase usando a credencial/acesstoken como referência
                {
                    if (task.IsCanceled)
                    {
                        //TODO Handle sign in cancel
#if DEV_MODE
                        Debug.Log("Sign in cancelled");
#endif
                        SignInAction(false);
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        //TODO handle sign in fault
#if DEV_MODE
                        Debug.Log("Sign in error: " + task.Exception);
#endif
                        SignInAction(false);
                        return;
                    }

                    FirebaseUser newUser = task.Result;
#if DEV_MODE
                    Debug.Log($"User signed in: {newUser.DisplayName}, {newUser.UserId}");
#endif
                    SignInAction(true);
                });
        }

        #endregion

        #region Google

        public void OnGoogleSignIn()
        {
            GoogleSignIn.Configuration = configuration;
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;
            AddStatusText("Calling SignIn");

            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
                OnAuthenticationFinished);
        }

        internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
        {
            if (task.IsFaulted)
            {
                using (IEnumerator<System.Exception> enumerator =
                    task.Exception.InnerExceptions.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        GoogleSignIn.SignInException error =
                            (GoogleSignIn.SignInException) enumerator.Current;
                        AddStatusText("Got Error: " + error.Status + " " + error.Message);
                    }
                    else
                    {
                        AddStatusText("Got Unexpected Exception?!?" + task.Exception);
                    }
                }
            }
            else if (task.IsCanceled)
            {
                AddStatusText("Canceled");
            }
            else
            {
                AddStatusText("Welcome: " + task.Result.DisplayName + "!");
                
                #if DEV_MODE
                Debug.Log("" + task.Result.IdToken);
                #endif
            }

            Firebase.Auth.Credential credential =
                Firebase.Auth.GoogleAuthProvider.GetCredential(task.Result.IdToken, task.Result.AuthCode);
            auth.SignInWithCredentialAsync(credential).ContinueWith(newTask =>
            {
                if (newTask.IsCanceled)
                {
                    #if DEV_MODE
                    Debug.LogError("SignInWithCredentialAsync was canceled.");
                    #endif
                    return;
                }

                if (newTask.IsFaulted)
                {
                    #if DEV_MODE
                    Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
                    #endif
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = newTask.Result;
                #if DEV_MODE
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);
                #endif
            });
        }

        private List<string> messages = new List<string>();

        void AddStatusText(string text)
        {
            if (messages.Count == 5)
            {
                messages.RemoveAt(0);
            }

            messages.Add(text);
            string txt = "";
            foreach (string s in messages)
            {
                txt += "\n" + s;
            }
#if DEV_MODE
            Debug.Log(txt);
#endif
        }

        #endregion

        #region Twitter

        public void LoginTwitter()
        {
            Debug.Log ("startLogin()");
            // To set API key navigate to tools->Twitter Kit
            Twitter.Init ();
		
            Twitter.LogIn (SignInTwitterFirebase, (ApiError error) => {
                LoginError(typeMessage = LoginReturn.twitter);
            });
        }

        public void SignInTwitterFirebase(TwitterSession session)
        {
            Firebase.Auth.Credential credential =
                Firebase.Auth.TwitterAuthProvider.GetCredential(session.authToken.token, session.authToken.secret);
            auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    #if DEV_MODE
                    Debug.Log("SignInWithCredentialAsync was canceled.");
                    #endif
                    return;
                }
                if (task.IsFaulted)
                {
                    #if DEV_MODE
                    Debug.Log("SignInWithCredentialAsync encountered an error: " + task.Exception);
                    #endif
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result;
                #if DEV_MODE
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);
                #endif
            });


            #if DEV_MODE
            Debug.Log(session.userName + " || " + session.userName + " || " + session.authToken.secret + " || " + session.authToken.token);
            #endif
        }

        #endregion

        #region Apple

        private void InitializeApple()
        {
            if (AppleAuthManager.IsCurrentPlatformSupported)
            {
                // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
                var deserializer = new PayloadDeserializer();
                // Creates an Apple Authentication manager with the deserializer
                this.appleAuthManager = new AppleAuthManager(deserializer);    
            }
        }

        public void SignInWithApple()
        {
            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

            this.appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    // Obtained credential, cast it to IAppleIDCredential
                    var appleIdCredential = credential as IAppleIDCredential;
                    if (appleIdCredential != null)
                    {
                        // Apple User ID
                        // You should save the user ID somewhere in the device
                        var userId = appleIdCredential.User;
                        //PlayerPrefs.SetString(AppleUserIdKey, userId);

                        // Email (Received ONLY in the first login)
                        var email = appleIdCredential.Email;

                        // Full name (Received ONLY in the first login)
                        var fullName = appleIdCredential.FullName;

                        // Identity token
                        var identityToken = Encoding.UTF8.GetString(
                            appleIdCredential.IdentityToken,
                            0,
                            appleIdCredential.IdentityToken.Length);

                        // Authorization code
                        var authorizationCode = Encoding.UTF8.GetString(
                            appleIdCredential.AuthorizationCode,
                            0,
                            appleIdCredential.AuthorizationCode.Length);

                        // And now you have all the information to create/login a user in your system
                    }
                },
                error =>
                {
                    // Something went wrong
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                });
            
//            PerformLoginWithAppleIdAndFirebase();
        }

        public void CheckCredentialRevoke()
        {
            this.appleAuthManager.SetCredentialsRevokedCallback(result =>
            {
                // Sign in with Apple Credentials were revoked.
                // Discard credentials/user id and go to login screen.
            });
        }

        public void CreatingNonce()
        {
            // Your custom Nonce string
            var yourCustomNonce = "RANDOM_NONCE_FORTHEAUTHORIZATIONREQUEST";
            var yourCustomState = "RANDOM_STATE_FORTHEAUTHORIZATIONREQUEST";

// Arguments for a normal Sign In With Apple Request
            var loginArgs = new AppleAuthLoginArgs(
                LoginOptions.IncludeEmail | LoginOptions.IncludeFullName,
                yourCustomNonce,
                yourCustomState);

// Arguments for a Quick Login
            var quickLoginArgs = new AppleAuthQuickLoginArgs(yourCustomNonce, yourCustomState);
        }
        
        private static string GenerateRandomString(int length)
        {
            if (length <= 0)
            {
                throw new Exception("Expected nonce to have positive length");
            }

            const string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvwxyz-._";
            var cryptographicallySecureRandomNumberGenerator = new RNGCryptoServiceProvider();
            var result = string.Empty;
            var remainingLength = length;

            var randomNumberHolder = new byte[1];
            while (remainingLength > 0)
            {
                var randomNumbers = new List<int>(16);
                for (var randomNumberCount = 0; randomNumberCount < 16; randomNumberCount++)
                {
                    cryptographicallySecureRandomNumberGenerator.GetBytes(randomNumberHolder);
                    randomNumbers.Add(randomNumberHolder[0]);
                }

                for (var randomNumberIndex = 0; randomNumberIndex < randomNumbers.Count; randomNumberIndex++)
                {
                    if (remainingLength == 0)
                    {
                        break;
                    }

                    var randomNumber = randomNumbers[randomNumberIndex];
                    if (randomNumber < charset.Length)
                    {
                        result += charset[randomNumber];
                        remainingLength--;
                    }
                }
            }

            return result;
        }
        
        private static string GenerateSHA256NonceFromRawNonce(string rawNonce)
        {
            var sha = new SHA256Managed();
            var utf8RawNonce = Encoding.UTF8.GetBytes(rawNonce);
            var hash = sha.ComputeHash(utf8RawNonce);

            var result = string.Empty;
            for (var i = 0; i < hash.Length; i++)
            {
                result += hash[i].ToString("x2");
            }

            return result;
        }
        
        public void PerformLoginWithAppleIdAndFirebase(Action<FirebaseUser> firebaseAuthCallback)
        {
            var rawNonce = GenerateRandomString(32);
            var nonce = GenerateSHA256NonceFromRawNonce(rawNonce);

            var loginArgs = new AppleAuthLoginArgs(
                LoginOptions.IncludeEmail | LoginOptions.IncludeFullName,
                nonce);

            this.appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    var appleIdCredential = credential as IAppleIDCredential;
                    if (appleIdCredential != null)
                    {
                        this.PerformFirebaseAuthentication(appleIdCredential, rawNonce, firebaseAuthCallback);
                    }
                },
                error =>
                {
                    // Something went wrong
                });
        }
        
        public void PerformQuickLoginWithFirebase(Action<FirebaseUser> firebaseAuthCallback)
        {
            var rawNonce = GenerateRandomString(32);
            var nonce = GenerateSHA256NonceFromRawNonce(rawNonce);

            var quickLoginArgs = new AppleAuthQuickLoginArgs(nonce);

            this.appleAuthManager.QuickLogin(
                quickLoginArgs,
                credential =>
                {
                    var appleIdCredential = credential as IAppleIDCredential;
                    if (appleIdCredential != null)
                    {
                        this.PerformFirebaseAuthentication(appleIdCredential, rawNonce, firebaseAuthCallback);
                    }
                },
                error =>
                {
                    // Something went wrong
                });
        }
        
        private void PerformFirebaseAuthentication(
            IAppleIDCredential appleIdCredential,
            string rawNonce,
            Action<FirebaseUser> firebaseAuthCallback)
        {
            var identityToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken);
            var authorizationCode = Encoding.UTF8.GetString(appleIdCredential.AuthorizationCode);
            var firebaseCredential = OAuthProvider.GetCredential(
                "apple.com",
                identityToken,
                rawNonce,
                authorizationCode);

            auth.SignInWithCredentialAsync(firebaseCredential)
                .ContinueWithOnMainThread(task => HandleSignInWithUser(task, firebaseAuthCallback));
        }

        private static void HandleSignInWithUser(Task<FirebaseUser> task, Action<FirebaseUser> firebaseUserCallback)
        {
            if (task.IsCanceled)
            {
                Debug.Log("Firebase auth was canceled");
                firebaseUserCallback(null);
            }
            else if (task.IsFaulted)
            {
                Debug.Log("Firebase auth failed");
                firebaseUserCallback(null);
            }
            else
            {
                var firebaseUser = task.Result;
                Debug.Log("Firebase auth completed | User ID:" + firebaseUser.UserId);
                firebaseUserCallback(firebaseUser);
            }
        }

        #endregion

        #region Anonymous

        public void LoginAnon()
        {
            auth.SignInAnonymouslyAsync().ContinueWith(task => {
                if (task.IsCanceled) {
                    Debug.LogError("SignInAnonymouslyAsync was canceled.");
                    return;
                }
                if (task.IsFaulted) {
                    Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);
            });
        }

        #endregion

        public void SignOut()
        {
            auth.SignOut();
        }
        
        public void OnDisconnect()
        {
            GoogleSignIn.DefaultInstance.Disconnect();
        }

        // Track state changes of the auth object.
        void AuthStateChanged(object sender, System.EventArgs eventArgs)
        {
            if (auth.CurrentUser != user)
            {
                bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
                if (!signedIn && user != null)
                {
                    Debug.Log("Signed out " + user.UserId);
                    currentUser = "";
                    SignedOut();
                }
                user = auth.CurrentUser;
                if (signedIn)
                {
                    Debug.Log("Signed in " + user.UserId);
                    currentUser = auth.CurrentUser.UserId;
                    SignedIn();
                }
            }
        }
        
        void OnDestroy()
        {
            auth.StateChanged -= AuthStateChanged;
            auth = null;
        }
    }
}