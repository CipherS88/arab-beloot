using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BotPlayer : PlayerController
{
    [Header("AI Settings")]
    public float thinkingTime = 1.0f; 

    public override IEnumerator DecideCut()
    {
        yield return new WaitForSeconds(thinkingTime);
        int choice = (Random.value > 0.5f) ? 1 : 2; 
        gameManager.OnUI_CutButton(choice);
    }
    public override IEnumerator DecideBuy(int round, GameManager.BuyType currentProject, GameManager.CardSuit currentSuit)
    {
        yield return new WaitForSeconds(thinkingTime);
        List<Card> myHandCards = gameManager.GetPlayerHand(mySeatIndex).Select(h => h.GetComponent<Card>()).ToList();
        int decision = BotBrain.EvaluateBuy(myHandCards, currentProject, gameManager.currentGroundCard, round);
        if (decision == 3) 
        {
            gameManager.playerBuyChoice = 3;
            // [FIXED] 
            GameManager.CardSuit groundSuit = (GameManager.CardSuit)(int)gameManager.currentGroundCard.suit;
            
            GameManager.CardSuit bestSuit = BotBrain.ChooseBestHokomSuit(myHandCards, groundSuit);
            gameManager.playerSuitChoice = (int)bestSuit;
        }
        else
        {
            gameManager.playerBuyChoice = decision;
        }
        
        gameManager.OnUI_BuyAction(decision);
        if (decision == 3) gameManager.OnUI_SuitSelect(gameManager.playerSuitChoice);
    }

    public override IEnumerator DecideProjects(List<GameManager.ProjectResult> myProjects)
    {
        yield return new WaitForSeconds(thinkingTime);
        foreach(var p in myProjects) gameManager.OnUI_ProjectClick(p.typeName);
        gameManager.OnUI_FinishDeclaring();
    }

    public override IEnumerator PlayCard(List<GameObject> myHand, List<Card> currentTrick)
    {
        yield return new WaitForSeconds(thinkingTime);

        List<Card> myHandCards = myHand.Select(h => h.GetComponent<Card>()).ToList();
        
        Card bestCard = BotBrain.ChooseBestCard(
            myHandCards, 
            currentTrick, 
            gameManager.playedCardsHistory, 
            mySeatIndex, 
            gameManager.currentGameType, 
            gameManager.currentGroundCard
        );

        myHand.Remove(bestCard.gameObject);
        gameManager.currentTrickCards.Add(bestCard);
        
        Debug.Log($"Bot {mySeatIndex} played SMART: {bestCard.suit} {bestCard.rank}");
    }
}