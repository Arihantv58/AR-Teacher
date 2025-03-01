using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    private Animator animator;

    // Define parameter names
    private string isTalkingParam = "Talk"; // Example bool parameter for talking
    

    void Start()
    {
        // Get the Animator component on the GameObject
        animator = GetComponent<Animator>();
    }

    public void StartTalkingAnimation()
    {
        // Set a bool parameter to true to trigger the talking animation
        animator.SetBool(isTalkingParam, true);
    }

    public void StopTalkingAnimation()
    {
        // Set the bool parameter to false to stop the talking animation
        animator.SetBool(isTalkingParam, false);
    }
}

