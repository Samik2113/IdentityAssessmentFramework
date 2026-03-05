namespace IamMaturityStudio.Web.Services;

public class AppState
{
    public Guid? CurrentAssessmentId { get; private set; }
    public bool HasUnsavedChanges { get; private set; }

    public event Action? Changed;

    public void SetAssessment(Guid assessmentId)
    {
        CurrentAssessmentId = assessmentId;
        Notify();
    }

    public void SetUnsavedChanges(bool hasUnsavedChanges)
    {
        HasUnsavedChanges = hasUnsavedChanges;
        Notify();
    }

    public void Clear()
    {
        CurrentAssessmentId = null;
        HasUnsavedChanges = false;
        Notify();
    }

    private void Notify() => Changed?.Invoke();
}
