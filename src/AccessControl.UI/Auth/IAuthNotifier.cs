namespace AccessControl.UI.Auth;

public interface IAuthNotifier
{
    void NotifyUserLoggedIn(string token);
    void NotifyUserLoggedOut();
}
