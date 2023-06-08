namespace Adr.Writing;

public class LiveKeyAction<T>
{
    public LiveKeyAction(char key, string description, Action<T> action)
    {
        Key = key;
        Description = description;
        Action = action;
    }

    public char Key { get; set; }

    public string Description { get; set; }

    public Action<T> Action { get; set; }
}
