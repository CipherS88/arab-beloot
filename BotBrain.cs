using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class BotBrain
{
    public static int EvaluateBuy(List<Card> hand, GameManager.BuyType currentProject, Card groundCard, int round)
    {
        if (HasWinningProject(hand)) return 1;

        if (CalculateSunHandStrength(hand) >= 20) return 1;

        if (currentProject == GameManager.BuyType.None)
        {
            if (round == 1)
            {
                if (IsGoodHokomHand(hand, (GameManager.CardSuit)(int)groundCard.suit)) return 2;
            }
            else if (round == 2)
            {
                foreach (GameManager.CardSuit suit in System.Enum.GetValues(typeof(GameManager.CardSuit)))
                {
                    if (suit == (GameManager.CardSuit)(int)groundCard.suit) continue;

                    if (IsGoodHokomHand(hand, suit))
                    {
                        return 3;
                    }
                }
            }
        }

        return 0;
    }

    static bool HasWinningProject(List<Card> hand)
    {
        var groups = hand.GroupBy(c => c.rank);
        foreach (var group in groups)
        {
            if (group.Count() == 4)
            {
                if (group.Key == Card.Rank.Ace) return true;
                if (group.Key == Card.Rank.King || group.Key == Card.Rank.Queen || 
                    group.Key == Card.Rank.Jack || group.Key == Card.Rank.Ten) return true;
            }
        }
        return false;
    }

    static bool IsGoodHokomHand(List<Card> hand, GameManager.CardSuit suit)
    {
        var suitCards = hand.Where(c => c.suit == (Card.Suit)(int)suit).ToList();
        
        bool hasJack = suitCards.Any(c => c.rank == Card.Rank.Jack);
        bool hasNine = suitCards.Any(c => c.rank == Card.Rank.Nine);
        bool hasAce = suitCards.Any(c => c.rank == Card.Rank.Ace);

        if (hasJack && hasNine) return true;
        if (hasJack && (hasAce || suitCards.Count >= 4)) return true;
        if (hasNine && hasAce && suitCards.Count >= 4) return true;

        return false;
    }

    static int CalculateSunHandStrength(List<Card> hand)
    {
        return hand.Sum(c => c.GetSunStrength());
    }

    public static GameManager.CardSuit ChooseBestHokomSuit(List<Card> hand, GameManager.CardSuit forbiddenSuit)
    {
        var groups = hand.GroupBy(c => c.suit).Where(g => g.Key != (Card.Suit)(int)forbiddenSuit);
        
        var bestSuitGroup = groups.OrderByDescending(g => g.Count() + (g.Any(c => c.rank == Card.Rank.Jack) ? 3 : 0)).FirstOrDefault();
        
        if (bestSuitGroup != null) return (GameManager.CardSuit)(int)bestSuitGroup.Key;
        
        return (GameManager.CardSuit)(((int)forbiddenSuit + 1) % 4); 
    }

    public static Card ChooseBestCard(List<Card> myHand, List<Card> currentTrick, List<Card> history, int mySeatIdx, GameManager.BuyType gameType, Card groundCard)
    {
        List<Card> validCards = new List<Card>();
        if (currentTrick.Count == 0) validCards = myHand;
        else
        {
            Card.Suit leadSuit = currentTrick[0].suit;
            validCards = myHand.Where(c => c.suit == leadSuit).ToList();
            if (validCards.Count == 0) validCards = myHand; 
        }

        if (currentTrick.Count == 0)
        {
            return ChooseLeadCard(myHand, history, mySeatIdx, gameType);
        }

        int currentWinnerIdx = GetCurrentTrickWinnerIndex(currentTrick, mySeatIdx); 
        int partnerIdx = (mySeatIdx + 2) % 4;
        bool partnerIsWinning = (currentWinnerIdx == partnerIdx);

        if (partnerIsWinning)
        {
            Card sharha = validCards.OrderByDescending(c => c.GetSunStrength()).FirstOrDefault();
            return sharha != null ? sharha : validCards[0];
        }
        else
        {
             return validCards.OrderBy(c => c.GetSunStrength()).First();
        }
    }

    static Card ChooseLeadCard(List<Card> hand, List<Card> history, int mySeatIdx, GameManager.BuyType gameType)
    {
        if (gameType == GameManager.BuyType.Sun)
        {
            var ace = hand.FirstOrDefault(c => c.rank == Card.Rank.Ace);
            if (ace != null) return ace;
        }
        return hand.OrderByDescending(c => c.GetSunStrength()).First();
    }
    
    static int GetCurrentTrickWinnerIndex(List<Card> trick, int mySeatIdx)
    {
        return (mySeatIdx - 1 + 4) % 4; 
    }
}