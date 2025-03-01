using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using System;
using System.Threading.Tasks;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using UnityEditor.VersionControl;
using Google;
using System.Net.Http;

public class FirebaseController : MonoBehaviour
{
    public GameObject loginPanel, signupPanel, profilePanel, forgetPasswordPanel, notificationPanel;
    public TMP_InputField loginEmail, loginPassword, signupEmail, signupPassword, signupCPassword, signupUsername, forgetPasswordEmail;
    public TextMeshProUGUI notification_title, notification_message, profileUserName, profileUserEmail;
    public Toggle rememberMe;
    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;
    private bool isFirebaseInitialized = false, isSignIn = false;
    private GoogleSignInConfiguration configuration;

    private void Awake()
    {
        // Configure the Google Web API key
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = "587327316244-stcnhr3lvm877kbog182nv29ggmdtkkl.apps.googleusercontent.com", // Replace with your actual Web Client ID
            RequestIdToken = true
        };
    }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("Firebase dependencies resolved.");
                InitializeFirebase();
                isFirebaseInitialized = true;
            }
            else
            {
                Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        });
    }

    public void GoogleSignInClick()
    {
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;
        GoogleSignIn.Configuration.RequestEmail = true;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(OnGoogleAuthenticatedFinished);
    }

    private void OnGoogleAuthenticatedFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
            Debug.LogError("Google Sign-In failed: " + task.Exception);
            showNotification("Error", "Google Sign-In failed. Please try again.");
        }
        else if (task.IsCanceled)
        {
            Debug.LogError("Google Sign-In was canceled.");
            showNotification("Error", "Google Sign-In was canceled.");
        }
        else
        {
            GoogleSignInUser googleUser = task.Result;
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCanceled)
                {
                    Debug.LogError("Firebase Sign-In with Google was canceled.");
                    return;
                }

                if (authTask.IsFaulted)
                {
                    Debug.LogError("Firebase Sign-In with Google encountered an error: " + authTask.Exception);
                    showNotification("Error", "Failed to sign in with Google. Please try again.");
                    return;
                }

                user = auth.CurrentUser;
                profileUserName.text = user.DisplayName;
                profileUserEmail.text = user.Email;
                loginPanel.SetActive(false);
                profilePanel.SetActive(true);

                showNotification("Success", "Signed in with Google successfully.");
            });
        }
    }


    public void OpenLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        forgetPasswordPanel.SetActive(false);
    }
    public void OpenSignupPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        profilePanel.SetActive(false);
        forgetPasswordPanel.SetActive(false);
    }
    public void OpenProfilePanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(true);
        forgetPasswordPanel.SetActive(false);
    }
    public void openForgetPasswordPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        forgetPasswordPanel.SetActive(true);
    }

    public void loginUser()
    {
        if (string.IsNullOrEmpty(loginEmail.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            showNotification("Error", "Fields are Empty! Please fill all the empty places.");
            return;
        }
        //Do Login
        signInUser(loginEmail.text, loginPassword.text);
    }
    public void signupUser()
    {
        if (string.IsNullOrEmpty(signupEmail.text) || string.IsNullOrEmpty(signupPassword.text) || string.IsNullOrEmpty(signupCPassword.text) || string.IsNullOrEmpty(signupUsername.text))
        {
            showNotification("Error", "Fields are Empty! Please fill all the empty places.");
            return;
        }
        //Do SignUp
        createUser(signupEmail.text, signupPassword.text, signupUsername.text);
    }

    public void forgetPassword()
    {
        if (string.IsNullOrEmpty(forgetPasswordEmail.text))
        {
            showNotification("Error", "Fields are Empty! Please fill all the empty places.");
            return;
        }
        forgetPasswordSubmit(forgetPasswordEmail.text);
    }
        
    private void showNotification(string title, string message)
    {
        notification_title.text = "" + title;
        notification_message.text = "" + message;
        notificationPanel.SetActive(true);
    }

    public void closeNotifPanel()
    {
        notification_title.text = "";
        notification_message.text = "";
        notificationPanel.SetActive(false);
    }

    public void logOut()
    {
        auth.SignOut();
        profileUserName.text = "";
        profileUserEmail.text = "";
        OpenLoginPanel();
    }

    void createUser(string email,  string password, string userName)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("Firebase is not initialized yet.");
            return;
        }
        if (auth == null)
        {
            Debug.LogError("FirebaseAuth instance is null. Please ensure Firebase is initialized.");
            return;
        }
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        Debug.LogError($"Firebase error code: {firebaseEx.ErrorCode}");
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        Debug.Log("Raw Firebase error code: " + firebaseEx.ErrorCode);
                        showNotification("Error", GetErrorMessage(errorCode));
                    }
                }
                return;
            }

            // Firebase user has been created.
            Firebase.Auth.AuthResult result = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);
            updateUserProfile(userName);
            OpenLoginPanel();
        });
    }

    public void signInUser(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        Debug.LogError($"Firebase error code: {firebaseEx.ErrorCode}");
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        Debug.Log("Raw Firebase error code: " + firebaseEx.ErrorCode);
                        showNotification("Error", GetErrorMessage(errorCode));
                    }
                }
                return;
            }

            Firebase.Auth.AuthResult result = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);
            profileUserName.text = "" + result.User.DisplayName;
            profileUserEmail.text = "" + result.User.Email;
            OpenProfilePanel();
        });
    }

    void InitializeFirebase()
    {
        try
        {
            auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

            if (auth == null)
            {
                Debug.LogError("FirebaseAuth instance is null after assignment.");
            }
            else
            {
                auth.StateChanged += AuthStateChanged;
                AuthStateChanged(this, null);
                Debug.Log("FirebaseAuth initialized successfully.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception during Firebase initialization: {e.Message}");
        }
    }



    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null
                && auth.CurrentUser.IsValid();
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
                isSignIn = true;
                
            }
        }
    }

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }

    void updateUserProfile(string userName)
    {
        Firebase.Auth.FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
            {
                DisplayName = userName,
                PhotoUrl = new System.Uri("https://dummyimage.com/150x150/000/fff.jpg"),
            };
            user.UpdateUserProfileAsync(profile).ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("UpdateUserProfileAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("UpdateUserProfileAsync encountered an error: " + task.Exception);
                    return;
                }

                Debug.Log("User profile updated successfully.");
                showNotification("Alert", "Account successfully updated.");
            });
        }
    }
    bool isSigned = false;

    private void Update()
    {
        if (isSignIn)
        {
            if (!isSigned)
            {
                isSigned = true;
                profileUserName.text = "" + user.DisplayName;
                profileUserEmail.text = "" + user.Email;
                OpenProfilePanel();
            }
        }
    }

    public void nextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private static string GetErrorMessage(AuthError errorCode)
    {
        Debug.Log("Received AuthError code: " + errorCode);

        // Ensure proper error handling by mapping Firebase error codes
        switch (errorCode)
        {
            case AuthError.AccountExistsWithDifferentCredentials:
                return "Account does not exists.";
            case AuthError.MissingPassword:
                return "Password is missing.";
            case AuthError.WeakPassword:
                return "Password is too weak. Try a stronger one.";
            case AuthError.WrongPassword:
                return "Incorrect password.";
            case AuthError.EmailAlreadyInUse:
                return "This email is already associated with another account.";
            case AuthError.InvalidEmail:
                return "The email address is invalid.";
            case AuthError.MissingEmail:
                return "Email address is missing.";
            default:
                return "An unknown error occurred.";
        }
    }
    void forgetPasswordSubmit(string forgetPasswordEmail)
    {
        auth.SendPasswordResetEmailAsync(forgetPasswordEmail).ContinueWithOnMainThread(task=>{
            if(task.IsCanceled)
            {
                Debug.LogError("SendPasswordResetEmailAsync was canceled.");
            }
            if(task.IsFaulted)
            {
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        Debug.LogError($"Firebase error code: {firebaseEx.ErrorCode}");
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        Debug.Log("Raw Firebase error code: " + firebaseEx.ErrorCode);
                        showNotification("Error", GetErrorMessage(errorCode));
                    }
                }
            }
            showNotification("Alert!", "Successfully sent Email for Reset Password.");
        }
        );
    }

}
