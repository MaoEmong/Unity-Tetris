using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingSlotUI : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text nameText;
    public TMP_Text scoreText;
    public Image outlineImage;

    public void Set(int rank, string name, int score)
    {
        rankText.text = rank.ToString();
        nameText.text = name;
        scoreText.text = score.ToString();

        // if(rank == 1)outlineImage.color = Color.yellow;
        // else if (rank == 2) outlineImage.color = Color.gray;
        // else if (rank == 3) outlineImage.color = Color.blue;
        // else outlineImage.color = Color.white;

    }
}
