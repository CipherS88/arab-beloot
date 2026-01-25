using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HumanPlayer : PlayerController
{

    // --------------------------------------------------------
    public override IEnumerator DecideCut()
    {
        gameManager.playerCutChoice = 0;
        if(gameManager.cutMenuPanel) gameManager.cutMenuPanel.SetActive(true);
        while (gameManager.playerCutChoice == 0) yield return null;
        if(gameManager.cutMenuPanel) gameManager.cutMenuPanel.SetActive(false);
    }

    public override IEnumerator DecideBuy(int round, GameManager.BuyType currentProject, GameManager.CardSuit currentSuit)
    {
        yield return gameManager.StartCoroutine(gameManager.EnableBuyUI());
        if (gameManager.playerBuyChoice == 3)
        {
             yield return gameManager.StartCoroutine(gameManager.EnableSuitSelectorUI());
        }
    }
    // --------------------------------------------------------
    public override IEnumerator DecideProjects(List<GameManager.ProjectResult> myProjects)
    {

        yield return gameManager.StartCoroutine(gameManager.EnableProjectUI(myProjects));
    }

    // --------------------------------------------------------
    public override IEnumerator PlayCard(List<GameObject> myHand, List<Card> currentTrick)
    {
        Debug.Log("Your Turn! Click a card.");
        foreach(var obj in myHand) 
        {
            if (obj != null) obj.GetComponent<Card>().isInteractable = true;
        }

        bool cardPlayed = false;
        while (!cardPlayed)
        {
            if (Input.GetMouseButtonDown(0))
            {

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Card c = hit.collider.GetComponent<Card>();
                    if (c != null && myHand.Contains(c.gameObject))
                    {

                        c.isInteractable = false;
                        myHand.Remove(c.gameObject);
                        gameManager.currentTrickCards.Add(c);
                        cardPlayed = true;
                    }
                }
            }
            yield return null;
        }
        foreach(var obj in myHand) 
        {
            if(obj != null) obj.GetComponent<Card>().isInteractable = false;
        }
    }
}