using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Configuration
    [Header("--- Seat Configuration (The Core) ---")]
    public PlayerController[] players = new PlayerController[4];

    [Header("--- Game Settings ---")]
    public bool disableBotsBuying = false; 
    [Range(0.1f, 3.0f)] public float animationSpeed = 0.5f;

    public enum BuyType { None, Sun, Hokom, HokomThani, Ashkal }
    public enum CardSuit { Spades, Hearts, Clubs, Diamonds }

    [Header("--- Setup References ---")]
    public List<GameObject> allCardPrefabs; 
    public Transform deckPosition;          
    public Transform groundCardPosition;    
    public Transform[] seatPositions;       
    
    [Header("--- Trick Piles (Where won cards go) ---")]
    public Transform[] trickPilePositions;

    [Header("--- UI References (Buying) ---")]
    public GameObject cutMenuPanel;
    public GameObject buyMenuPanel;
    public GameObject suitSelectionPanel;

    [Header("--- UI References (Projects) ---")]
    public GameObject projectsPanel; 
    public Button btn400, btn100, btnSra, btn50, btnOkProjects;

    [Header("--- UI References (Result) ---")]
    public SimpleResultUI simpleResultUI;

    [Header("--- Action Buttons ---")]
    public Button btnSun, btnHokom, btnAshkal, btnHokom2, btnPass;
    public Button[] btnSuits; 

    [Header("--- Visual Tuning ---")]
    [Range(0.0f, 5.0f)] public float handSpacing = 0.4f;
    [Range(0.0f, 20.0f)] public float fanAngle = 10f;
    [Range(0.0f, 0.1f)] public float arcHeight = 0.02f;
    [Range(0.0f, 0.05f)] public float depthOffset = 0.005f;
    [Range(0.0f, 5.0f)] public float mukabaraScatterRadius = 2.5f;

    private List<GameObject> spawnedCards = new List<GameObject>();
    private List<GameObject>[] playerHands = new List<GameObject>[4];
    private List<Card> selectedMukabaraCards = new List<Card>();
    
    public Card currentGroundCard; 
    public List<Card> playedCardsHistory = new List<Card>(); 

    private int dealerIndex;
    public int currentBuyerIndex;
    private int buyRoundNumber = 1;
    private int passCount = 0;
    private bool isWaitingForMukabaraPick = false; 

    public BuyType currentGameType = BuyType.None;
    public CardSuit currentHokomSuit;
    public int buyerPlayerID = -1;

    [HideInInspector] public int playerCutChoice = 0; 
    [HideInInspector] public int playerBuyChoice = -1; 
    [HideInInspector] public int playerSuitChoice = -1; 
    [HideInInspector] public bool playerFinishedProjectDeclaring = false;

    [Header("--- Game Logic (Runtime) ---")]
    public int teamAScore = 0; 
    public int teamBScore = 0; 
    public List<Card> currentTrickCards = new List<Card>(); 
    private int currentTurnPlayer; 

    [System.Serializable]
    public class ProjectResult
    {
        public string typeName; 
        public int score;       
        public int highRank;    
        public string description;
        public int ownerIndex; 
    }
    private List<ProjectResult> declaredProjectsGlobal = new List<ProjectResult>();

    // Initialization
    void Awake()
    {
        Instance = this;
        for(int i=0; i<4; i++) playerHands[i] = new List<GameObject>();
    }

    void Start()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null) players[i].Setup(i, this);
            else Debug.LogError($"Player Controller for Seat {i} is MISSING!");
        }

        if(cutMenuPanel) cutMenuPanel.SetActive(false);
        if(buyMenuPanel) buyMenuPanel.SetActive(false);
        if(suitSelectionPanel) suitSelectionPanel.SetActive(false);
        if(projectsPanel) projectsPanel.SetActive(false);

        StartCoroutine(GameLoop());
    }

    // Loop
    IEnumerator GameLoop()
    {
        SpawnDeck();
        yield return new WaitForSeconds(1f);

        int tempDealer = -1;
        while (tempDealer == -1)
        {
            yield return StartCoroutine(MukabaraPhase());
            tempDealer = DetermineMukabaraWinner();
            if (tempDealer == -1) { 
                Debug.Log("Tie in Mukabara! Restarting...");
                yield return StartCoroutine(GatherCardsToDeck()); 
                yield return new WaitForSeconds(1f); 
            }
        }
        dealerIndex = tempDealer;
        Debug.Log($"Dealer is Player {dealerIndex}");

        yield return StartCoroutine(GatherCardsToDeck());
        yield return new WaitForSeconds(0.5f);

        ShuffleList(spawnedCards);
        int cutterIndex = (dealerIndex + 1) % 4;
        playerCutChoice = 0; 
        yield return StartCoroutine(players[cutterIndex].DecideCut());

        if (playerCutChoice == 1) yield return StartCoroutine(PerformVisualCut());

        yield return StartCoroutine(DealPhase(dealerIndex));

        RevealGroundCard();
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(BuyingPhase());

        if (currentGameType != BuyType.None)
        {
            yield return StartCoroutine(DistributeRestOfCards());
            yield return StartCoroutine(ProjectDeclarationPhase());

            Debug.Log("--- GAME STARTING ---");
            yield return StartCoroutine(PlayingPhase()); 
        }
        else
        {
            Debug.Log("No Buy (Wala). Restarting Round...");
        }
    }

    // Workflow
    IEnumerator MukabaraPhase()
    {
        selectedMukabaraCards.Clear();
        ShuffleList(spawnedCards);

        foreach (var cObj in spawnedCards)
        {
            Card c = cObj.GetComponent<Card>();
            c.isInteractable = true; 
            Vector2 rnd = Random.insideUnitCircle * mukabaraScatterRadius;
            Vector3 targetPos = deckPosition.position + new Vector3(rnd.x, 0, rnd.y);
            Quaternion targetRot = Quaternion.Euler(0, Random.Range(0, 360), 180);
            c.MoveTo(targetPos, targetRot, animationSpeed);
            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(0.5f);
        Debug.Log("Waiting for Human to pick a card...");
        isWaitingForMukabaraPick = true;
        yield return new WaitUntil(() => !isWaitingForMukabaraPick);

        List<GameObject> available = spawnedCards.Where(x => !selectedMukabaraCards.Contains(x.GetComponent<Card>())).ToList();
        for(int i=1; i<4; i++) 
        {
            yield return new WaitForSeconds(0.2f);
            if (available.Count > 0)
            {
                GameObject picked = available[Random.Range(0, available.Count)];
                available.Remove(picked);
                MoveMukabaraCardToSeat(picked.GetComponent<Card>(), i);
            }
        }
        yield return new WaitForSeconds(0.5f);

        foreach (var c in selectedMukabaraCards)
        {
            float currentY = c.transform.eulerAngles.y;
            c.MoveTo(c.transform.position, Quaternion.Euler(0, currentY, 0), 0.5f); 
        }
        yield return new WaitForSeconds(2.0f);
    }

    public void OnCardClicked(Card card)
    {
        if (isWaitingForMukabaraPick)
        {
            if (!selectedMukabaraCards.Contains(card))
            {
                MoveMukabaraCardToSeat(card, 0); 
                isWaitingForMukabaraPick = false; 
                foreach(var c in spawnedCards) c.GetComponent<Card>().isInteractable = false;
            }
        }
        else
        {
            Debug.Log($"Card Clicked: {card.suit} - {card.rank}");
        }
    }

    IEnumerator BuyingPhase()
    {
        currentBuyerIndex = (dealerIndex + 1) % 4;
        buyRoundNumber = 1;
        passCount = 0;
        currentGameType = BuyType.None;
        buyerPlayerID = -1;
        bool buyFinished = false;

        while (!buyFinished)
        {
            playerBuyChoice = -1; 
            playerSuitChoice = -1;
            yield return StartCoroutine(players[currentBuyerIndex].DecideBuy(buyRoundNumber, currentGameType, currentHokomSuit));
            
            BuyType decision = BuyType.None;
            CardSuit chosenSuit = CardSuit.Spades;

            if (playerBuyChoice == 1) decision = BuyType.Sun;
            else if (playerBuyChoice == 2) { decision = BuyType.Hokom; chosenSuit = (CardSuit)(int)currentGroundCard.suit; }
            else if (playerBuyChoice == 4) decision = BuyType.Ashkal;
            else if (playerBuyChoice == 3) 
            {
                decision = BuyType.HokomThani;
                chosenSuit = (CardSuit)playerSuitChoice; 
            }
            else decision = BuyType.None; 

            if (decision == BuyType.Sun)
            {
                currentGameType = BuyType.Sun;
                buyerPlayerID = currentBuyerIndex;
                buyFinished = true;
            }
            else if (decision == BuyType.Ashkal)
            {
                currentGameType = BuyType.Ashkal;
                buyerPlayerID = (currentBuyerIndex + 2) % 4; 
                buyFinished = true;
            }
            else if (decision == BuyType.Hokom || decision == BuyType.HokomThani)
            {
                if (currentGameType == BuyType.None)
                {
                    currentGameType = decision;
                    buyerPlayerID = currentBuyerIndex;
                    currentHokomSuit = chosenSuit;
                    buyFinished = true; 
                }
            }
            else passCount++;

            if (currentGameType == BuyType.Sun) buyFinished = true;

            if (!buyFinished)
            {
                currentBuyerIndex = (currentBuyerIndex + 1) % 4;
                if (passCount >= 4 && currentGameType == BuyType.None)
                {
                    if (buyRoundNumber == 1) { buyRoundNumber = 2; passCount = 0; currentBuyerIndex = (dealerIndex + 1) % 4; }
                    else { buyFinished = true; }
                }
            }
        }
    }

    IEnumerator ProjectDeclarationPhase()
    {
        Debug.Log("--- Declaring Projects Phase ---");
        declaredProjectsGlobal.Clear();
        int startPlayer = (dealerIndex + 1) % 4;
        
        for (int i = 0; i < 4; i++)
        {
            int curr = (startPlayer + i) % 4;
            List<ProjectResult> available = CheckProjects(curr);
            if (available.Count > 0)
            {
                yield return StartCoroutine(players[curr].DecideProjects(available));
            }
        }
        CompareAndFinalizeProjects();
    }

    IEnumerator PlayingPhase()
    {
        Debug.Log("--- STARTING PLAY PHASE ---");
        int startPlayer = (dealerIndex + 1) % 4; 
        
        float[] pileHeights = new float[4]; 
        int lastTrickWinnerIndex = -1;

        teamAScore = 0; 
        teamBScore = 0;

        for (int trick = 0; trick < 8; trick++)
        {
            Debug.Log($"--- Trick {trick + 1} / 8 ---");
            currentTrickCards.Clear();
            
            for (int i = 0; i < 4; i++)
            {
                int playerIdx = (startPlayer + i) % 4;
                currentTurnPlayer = playerIdx;
                
                yield return StartCoroutine(players[playerIdx].PlayCard(playerHands[playerIdx], currentTrickCards));

                ArrangeHand(playerIdx);

                if (currentTrickCards.Count > i)
                {
                     Card playedCard = currentTrickCards[i];
                     Vector2 rnd = Random.insideUnitCircle * 0.3f;
                     Vector3 targetPos = groundCardPosition.position + new Vector3(rnd.x, 0.02f * i, rnd.y);
                     Quaternion targetRot = Quaternion.Euler(0, Random.Range(-20, 20), 0);
                     playedCard.MoveTo(targetPos, targetRot, 0.3f);
                     yield return new WaitForSeconds(0.5f);
                }
            }

            int winnerLocalIndex = EvaluateTrickWinner(currentTrickCards); 
            int absoluteWinnerIndex = (startPlayer + winnerLocalIndex) % 4;
            lastTrickWinnerIndex = absoluteWinnerIndex; 

            Debug.Log($"Player {absoluteWinnerIndex} WINS the trick!");
            yield return new WaitForSeconds(0.5f);

            foreach(var c in currentTrickCards)
            {
                Transform pileTr = trickPilePositions[absoluteWinnerIndex];
                Vector2 pileRnd = Random.insideUnitCircle * 0.2f; 
                pileHeights[absoluteWinnerIndex] += 0.01f; 
                Vector3 pilePos = pileTr.position + new Vector3(pileRnd.x, pileHeights[absoluteWinnerIndex], pileRnd.y);
                Quaternion pileRot = Quaternion.Euler(0, Random.Range(0, 360), 180);
                c.MoveTo(pilePos, pileRot, 0.4f);
            }

            int trickScore = CalculateTrickScore(currentTrickCards);
            if (absoluteWinnerIndex == 0 || absoluteWinnerIndex == 2) teamAScore += trickScore;
            else teamBScore += trickScore;

            Debug.Log($"Score Update: Team A: {teamAScore} | Team B: {teamBScore}");
            yield return new WaitForSeconds(0.5f);
            startPlayer = absoluteWinnerIndex;
        }

        int groundPointsA = (lastTrickWinnerIndex == 0 || lastTrickWinnerIndex == 2) ? 10 : 0;
        int groundPointsB = (lastTrickWinnerIndex == 1 || lastTrickWinnerIndex == 3) ? 10 : 0;

        int projectsA = declaredProjectsGlobal.Where(p => p.ownerIndex == 0 || p.ownerIndex == 2).Sum(p => p.score);
        int projectsB = declaredProjectsGlobal.Where(p => p.ownerIndex == 1 || p.ownerIndex == 3).Sum(p => p.score);

        string typeText = (currentGameType == BuyType.Sun) ? "صن" : "حكم";

        Debug.Log("--- GAME OVER --- Showing Results UI");
        yield return new WaitForSeconds(1f);

        if (simpleResultUI != null)
        {
            simpleResultUI.ShowSimpleResult(
                typeText,
                teamAScore, teamBScore, 
                groundPointsA, groundPointsB, 
                projectsA, projectsB 
            );
        }
        else
        {
            Debug.LogWarning("SimpleResultUI is not linked in GameManager Inspector!");
        }
    }

    // Interface
    public List<GameObject> GetPlayerHand(int seatIndex)
    {
        return playerHands[seatIndex];
    }

    public IEnumerator EnableBuyUI()
    {
        playerBuyChoice = -1;
        buyMenuPanel.SetActive(true);
        if(btnSun) btnSun.gameObject.SetActive(true);
        if(btnHokom) btnHokom.gameObject.SetActive(buyRoundNumber == 1);
        if(btnHokom2) btnHokom2.gameObject.SetActive(buyRoundNumber == 2);
        
        int myRelPos = (0 - dealerIndex + 4) % 4;
        bool canAshkal = (myRelPos == 3) || (myRelPos == 0 && buyRoundNumber == 2);
        if(btnAshkal) btnAshkal.gameObject.SetActive(canAshkal);
        if(btnPass) btnPass.gameObject.SetActive(true);

        yield return new WaitUntil(() => playerBuyChoice != -1);
        buyMenuPanel.SetActive(false);
    }

    public IEnumerator EnableSuitSelectorUI()
    {
        playerSuitChoice = -1;
        suitSelectionPanel.SetActive(true);
        if (currentGroundCard != null)
        {
            int groundIdx = (int)currentGroundCard.suit;
            for(int i=0; i<btnSuits.Length; i++) 
                if(btnSuits[i]) btnSuits[i].gameObject.SetActive(i != groundIdx);
        }
        yield return new WaitUntil(() => playerSuitChoice != -1);
        suitSelectionPanel.SetActive(false);
    }
    
    public IEnumerator EnableProjectUI(List<ProjectResult> myProjects)
    {
        playerFinishedProjectDeclaring = false;
        projectsPanel.SetActive(true);
        if(btn400) btn400.interactable = false;
        if(btn100) btn100.interactable = false;
        if(btn50) btn50.interactable = false;
        if(btnSra) btnSra.interactable = false;

        foreach (var p in myProjects)
        {
            if (p.typeName == "400" && btn400) btn400.interactable = true;
            if (p.typeName == "100" && btn100) btn100.interactable = true;
            if (p.typeName == "50" && btn50) btn50.interactable = true;
            if (p.typeName == "Sra" && btnSra) btnSra.interactable = true;
        }
        yield return new WaitUntil(() => playerFinishedProjectDeclaring);
        projectsPanel.SetActive(false);
    }

    public void OnUI_BuyAction(int actionID) { playerBuyChoice = actionID; }
    public void OnUI_SuitSelect(int suitID) { playerSuitChoice = suitID; }
    public void OnUI_CutButton(int choice) { playerCutChoice = choice; }
    public void OnUI_FinishDeclaring() { playerFinishedProjectDeclaring = true; }
    
    public void OnUI_ProjectClick(string projectType)
    {
        List<ProjectResult> realProjects = CheckProjects(0); 
        ProjectResult found = realProjects.Find(p => p.typeName == projectType);
        if (found != null)
        {
            found.ownerIndex = 0;
            declaredProjectsGlobal.Add(found);
            if(projectType=="Sra" && btnSra) btnSra.interactable = false;
            if(projectType=="50" && btn50) btn50.interactable = false;
            if(projectType=="100" && btn100) btn100.interactable = false;
            if(projectType=="400" && btn400) btn400.interactable = false;
        }
    }

    // Utilities
    void SpawnDeck()
    {
        foreach(var p in allCardPrefabs) {
            var c = Instantiate(p, deckPosition.position, Quaternion.Euler(0,0,180));
            spawnedCards.Add(c);
        }
    }

    void MoveMukabaraCardToSeat(Card card, int seatIndex)
    {
        card.isInteractable = false;
        selectedMukabaraCards.Add(card);
        Transform seat = seatPositions[seatIndex];
        Vector3 targetPos = seat.position + (seat.forward * 1.5f);
        float yRot = 0;
        switch(seatIndex) { case 0: yRot=0; break; case 1: yRot=-90; break; case 2: yRot=180; break; case 3: yRot=90; break; }
        card.MoveTo(targetPos, Quaternion.Euler(0, yRot, 180), animationSpeed);
    }

    int DetermineMukabaraWinner()
    {
        int max = -1;
        List<int> winners = new List<int>();
        for(int i=0; i<selectedMukabaraCards.Count; i++) {
            int val = selectedMukabaraCards[i].GetSunStrength();
            if(val > max) { max = val; winners.Clear(); winners.Add(i); }
            else if (val == max) winners.Add(i);
        }
        return (winners.Count == 1) ? winners[0] : -1;
    }

    IEnumerator GatherCardsToDeck()
    {
        foreach(var c in spawnedCards)
            c.GetComponent<Card>().MoveTo(deckPosition.position, Quaternion.Euler(0,0,180), 0.3f);
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator PerformVisualCut()
    {
         int cutPoint = spawnedCards.Count / 2;
         List<GameObject> topHalf = spawnedCards.GetRange(cutPoint, spawnedCards.Count - cutPoint);
         List<GameObject> bottomHalf = spawnedCards.GetRange(0, cutPoint);

         foreach (var card in topHalf) card.GetComponent<Card>().MoveTo(card.transform.position + Vector3.up * 0.5f, card.transform.rotation, 0.3f);
         yield return new WaitForSeconds(0.35f);
         foreach (var card in topHalf) card.GetComponent<Card>().MoveTo(card.transform.position + Vector3.right * 1.0f, card.transform.rotation, 0.3f);
         yield return new WaitForSeconds(0.35f);

         spawnedCards.Clear();
         spawnedCards.AddRange(bottomHalf);
         spawnedCards.AddRange(topHalf);

         yield return StartCoroutine(GatherCardsToDeck());
    }

    IEnumerator DealPhase(int dealerIdx)
    {
        int start = (dealerIdx + 1) % 4;
        int idx = 0;
        int[] patterns = {3, 2};
        foreach(int count in patterns) {
            for(int i=0; i<4; i++) {
                int curr = (start + i) % 4;
                for(int c=0; c<count; c++) {
                    if(idx < spawnedCards.Count) {
                        playerHands[curr].Add(spawnedCards[idx]);
                        ArrangeHand(curr);
                        idx++;
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void RevealGroundCard()
    {
        if (spawnedCards.Count > 20) {
            GameObject g = spawnedCards[20];
            currentGroundCard = g.GetComponent<Card>();
            currentGroundCard.MoveTo(groundCardPosition.position, Quaternion.identity, 0.5f);
        }
    }

    IEnumerator DistributeRestOfCards()
    {
        if (currentGroundCard != null) {
            playerHands[buyerPlayerID].Add(currentGroundCard.gameObject);
            ArrangeHand(buyerPlayerID);
        }
        
        int currentCardIndex = 21;
        int startPlayer = (dealerIndex + 1) % 4;
        for (int i = 0; i < 4; i++) {
            int playerIdx = (startPlayer + i) % 4;
            int cardsToGive = (playerIdx == buyerPlayerID) ? 2 : 3;
            
            for (int c = 0; c < cardsToGive; c++) {
                if (currentCardIndex < spawnedCards.Count) {
                    playerHands[playerIdx].Add(spawnedCards[currentCardIndex]);
                    ArrangeHand(playerIdx);
                    currentCardIndex++;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    void ShuffleList(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++) {
            GameObject temp = list[i];
            int r = Random.Range(i, list.Count);
            list[i] = list[r];
            list[r] = temp;
        }
    }

    void SortHandList(int seatIdx)
    {
        List<GameObject> hand = playerHands[seatIdx];
        hand.Sort((objA, objB) => 
        {
            Card cardA = objA.GetComponent<Card>();
            Card cardB = objB.GetComponent<Card>();

            if (cardA.suit != cardB.suit)
                return cardA.suit.CompareTo(cardB.suit);

            int strengthA = GetCardSortStrength(cardA);
            int strengthB = GetCardSortStrength(cardB);
            return strengthB.CompareTo(strengthA);
        });
    }

    int GetCardSortStrength(Card c)
    {
        bool isHokomRound = (currentGameType == BuyType.Hokom || currentGameType == BuyType.HokomThani);
        bool isTrumpSuit = isHokomRound && (c.suit == (Card.Suit)(int)currentHokomSuit);

        if (isTrumpSuit)
        {
            switch (c.rank)
            {
                case Card.Rank.Jack: return 20;
                case Card.Rank.Nine: return 19;
                case Card.Rank.Ace: return 18;
                case Card.Rank.Ten: return 17;
                case Card.Rank.King: return 16;
                case Card.Rank.Queen: return 15;
                case Card.Rank.Eight: return 14;
                default: return 13; 
            }
        }
        else
        {
            switch (c.rank)
            {
                case Card.Rank.Ace: return 10;
                case Card.Rank.Ten: return 9;
                case Card.Rank.King: return 8;
                case Card.Rank.Queen: return 7;
                case Card.Rank.Jack: return 6;
                case Card.Rank.Nine: return 5;
                case Card.Rank.Eight: return 4;
                default: return 3; 
            }
        }
    }

    void ArrangeHand(int pIdx) 
    {
        SortHandList(pIdx); 

        List<GameObject> hand = playerHands[pIdx];
        Transform seat = seatPositions[pIdx];
        int count = hand.Count;
        
        bool isMe = (pIdx == 0); 
        float currentSpacing = isMe ? handSpacing * 3.0f : handSpacing * 0.8f;
        float totalWidth = (count - 1) * currentSpacing;
        float startX = -totalWidth / 2f;

        Vector3 spreadDir = Vector3.right; 
        Vector3 faceDir = Vector3.forward; 
        float seatYRotation = 0;

        switch(pIdx) {
            case 0: spreadDir = Vector3.right; faceDir = Vector3.forward; seatYRotation = 0; break;
            case 1: spreadDir = Vector3.forward; faceDir = Vector3.left; seatYRotation = -90; break;
            case 2: spreadDir = Vector3.left; faceDir = Vector3.back; seatYRotation = 180; break;
            case 3: spreadDir = Vector3.back; faceDir = Vector3.right; seatYRotation = 90; break;
        }

        for (int i = 0; i < count; i++) {
            if(hand[i] == null) continue;
            Card cardScript = hand[i].GetComponent<Card>();
            
            float xOffset = startX + (i * currentSpacing);
            float normalizedPos = (count > 1) ? (float)i / (count - 1) : 0.5f;
            float archCurve = 4 * normalizedPos * (1 - normalizedPos); 
            float zOffset = (i * depthOffset) + (archCurve * arcHeight);
            
            Vector3 mobileOffset = isMe ? (faceDir * 2.5f) + (Vector3.up * 0.5f) : Vector3.zero;
            Vector3 finalPos = seat.position + (spreadDir * xOffset) + (faceDir * zOffset) + mobileOffset;
            
            Quaternion baseRot = isMe ? Quaternion.Euler(0, seatYRotation, 0) : Quaternion.Euler(0, seatYRotation, 180);
            float modifiedFanAngle = isMe ? fanAngle * 0.5f : fanAngle;
            float rotAngle = Mathf.Lerp(-modifiedFanAngle, modifiedFanAngle, normalizedPos);
            Quaternion finalRot = baseRot * Quaternion.Euler(0, rotAngle, 0);
            
            cardScript.MoveTo(finalPos, finalRot, animationSpeed);
            
            if (isMe) { 
                Vector3 sortOffset = faceDir * ((i) * 0.02f); 
                cardScript.transform.position -= sortOffset; 
            }
        }
    }

    // Logic
    int CalculateTrickScore(List<Card> cards)
    {
        int score = 0;
        foreach (var c in cards)
        {
            if (currentGameType == BuyType.Sun) score += c.GetSunStrength(); 
        }
        return score;
    }

    int EvaluateTrickWinner(List<Card> trick)
    {
        Card leadCard = trick[0];
        int winnerIdx = 0;
        Card currentWinningCard = leadCard;

        for (int i = 1; i < trick.Count; i++)
        {
            Card challenger = trick[i];
            
            if (currentGameType == BuyType.Sun)
            {
                if (challenger.suit == currentWinningCard.suit)
                {
                    if (challenger.GetSunStrength() > currentWinningCard.GetSunStrength()) 
                    {
                        winnerIdx = i; currentWinningCard = challenger;
                    }
                    else if (challenger.GetSunStrength() == currentWinningCard.GetSunStrength())
                    {
                        if (challenger.rank > currentWinningCard.rank) 
                        {
                            winnerIdx = i; currentWinningCard = challenger;
                        }
                    }
                }
            }
        }
        return winnerIdx;
    }

    public List<ProjectResult> CheckProjects(int playerIndex)
    {
        List<GameObject> handObjs = playerHands[playerIndex];
        List<Card> hand = handObjs.Select(h => h.GetComponent<Card>()).ToList();
        List<ProjectResult> foundProjects = new List<ProjectResult>();

        var rankGroups = hand.GroupBy(c => c.rank);
        foreach (var group in rankGroups)
        {
            if (group.Count() == 4)
            {
                Card.Rank r = group.Key;
                if (r == Card.Rank.Ace && currentGameType == BuyType.Sun)
                    foundProjects.Add(new ProjectResult { typeName = "400", score = 400, highRank = (int)r, description = "Four Aces (Sun)", ownerIndex = playerIndex });
                else if (r == Card.Rank.Ace || r == Card.Rank.King || r == Card.Rank.Queen || r == Card.Rank.Jack || r == Card.Rank.Ten)
                    foundProjects.Add(new ProjectResult { typeName = "100", score = 100, highRank = (int)r, description = "Four of a Kind", ownerIndex = playerIndex });
            }
        }

        var suitGroups = hand.GroupBy(c => c.suit);
        foreach (var sGroup in suitGroups)
        {
            List<Card> sortedCards = sGroup.OrderBy(c => c.rank).ToList();
            int currentSeq = 1;
            for (int i = 0; i < sortedCards.Count - 1; i++)
            {
                if ((int)sortedCards[i+1].rank == (int)sortedCards[i].rank + 1) currentSeq++;
                else 
                { 
                    AddSequenceProject(foundProjects, currentSeq, sortedCards[i], playerIndex); 
                    currentSeq = 1; 
                }
            }
            AddSequenceProject(foundProjects, currentSeq, sortedCards.Last(), playerIndex);
        }

        if (currentGameType == BuyType.Hokom || currentGameType == BuyType.HokomThani)
        {
            CardSuit hSuit = (currentGameType == BuyType.Hokom) ? (CardSuit)currentGroundCard.suit : currentHokomSuit;
            bool hasKing = hand.Any(c => c.suit == (Card.Suit)hSuit && c.rank == Card.Rank.King);
            bool hasQueen = hand.Any(c => c.suit == (Card.Suit)hSuit && c.rank == Card.Rank.Queen);
            if (hasKing && hasQueen) 
                foundProjects.Add(new ProjectResult { typeName = "Baloot", score = 20, highRank = 0, description = "King & Queen", ownerIndex = playerIndex });
        }
        return foundProjects;
    }

    void AddSequenceProject(List<ProjectResult> list, int count, Card highestCard, int owner)
    {
        if (count == 3) list.Add(new ProjectResult { typeName = "Sra", score = 20, highRank = (int)highestCard.rank, description = "Sira", ownerIndex = owner });
        else if (count == 4) list.Add(new ProjectResult { typeName = "50", score = 50, highRank = (int)highestCard.rank, description = "Fifty", ownerIndex = owner });
        else if (count >= 5) list.Add(new ProjectResult { typeName = "100", score = 100, highRank = (int)highestCard.rank, description = "Sequence 100", ownerIndex = owner });
    }

    void CompareAndFinalizeProjects()
    {
        if (declaredProjectsGlobal.Count == 0) return;
        Debug.Log("--- Comparing Projects ---");
        
        List<ProjectResult> teamA = declaredProjectsGlobal.Where(p => p.ownerIndex == 0 || p.ownerIndex == 2).ToList();
        List<ProjectResult> teamB = declaredProjectsGlobal.Where(p => p.ownerIndex == 1 || p.ownerIndex == 3).ToList();

        if (teamA.Count == 0 && teamB.Count == 0) return;
        if (teamA.Count > 0 && teamB.Count == 0) { Debug.Log("Team A Projects Win"); return; }
        if (teamB.Count > 0 && teamA.Count == 0) { Debug.Log("Team B Projects Win"); return; }

        ProjectResult bestA = GetBestProject(teamA);
        ProjectResult bestB = GetBestProject(teamB);
        
        int winnerTeam = -1; 
        if (bestA.score > bestB.score) winnerTeam = 0;
        else if (bestB.score > bestA.score) winnerTeam = 1;
        else 
        {
            if (bestA.highRank > bestB.highRank) winnerTeam = 0;
            else if (bestB.highRank > bestA.highRank) winnerTeam = 1;
            else 
            {
                int distA = (bestA.ownerIndex - (dealerIndex + 1) + 4) % 4;
                int distB = (bestB.ownerIndex - (dealerIndex + 1) + 4) % 4;
                if (distA < distB) winnerTeam = 0; else winnerTeam = 1;
            }
        }
        
        Debug.Log($"Winner Team for Projects: {(winnerTeam==0 ? "Team A" : "Team B")}");
    }

    ProjectResult GetBestProject(List<ProjectResult> projects)
    {
        return projects.OrderByDescending(p => p.score).ThenByDescending(p => p.highRank).First();
    }
}