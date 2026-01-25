using UnityEngine;

public class Card : MonoBehaviour
{
    public enum Suit { Spades = 0, Hearts = 1, Clubs = 2, Diamonds = 3 }
    public enum Rank { Seven=0, Eight=1, Nine=2, Ten=3, Jack=4, Queen=5, King=6, Ace=7 }
    [Header("Card Data")]
    public Suit suit;
    public Rank rank;
    public bool isFaceUp = false;
    public bool isInteractable = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float moveSpeed;
    private bool isMoving = false;
    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed * 5f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed * 5f);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                isMoving = false;
            }
        }
    }

    public void MoveTo(Vector3 pos, Quaternion rot, float duration)
    {
        targetPosition = pos;
        targetRotation = rot;
        moveSpeed = 1f / duration;
        isMoving = true;
    }
    public int GetSunStrength()
    {
        switch (rank)
        {
            case Rank.Ace: return 11;
            case Rank.Ten: return 10;
            case Rank.King: return 4;
            case Rank.Queen: return 3;
            case Rank.Jack: return 2;
            default: return 0;
        }
    }
    public int GetProjectRankValue()
    {
        return (int)rank; 
    }
    void OnMouseDown()
    {
        if (isInteractable && GameManager.Instance != null)
        {
            GameManager.Instance.OnCardClicked(this);
        }
    }
}