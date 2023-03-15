using System.Net;
using UnityEngine;
using TMPro;

public class ConnectivityChecker : MonoBehaviour
{
    private Pinger pinger;
    private int failedPingCount;
    private float timer = 0f;
    private bool decreasing = true;

    [SerializeField] TextMeshProUGUI textObject;

    private void Start() { this.pinger = new Pinger(); }

    private async void FixedUpdate()
    {
        this.timer += Time.deltaTime;

        // If the timer has run for more than 3s, ping the database again and reset the timer
        if (this.timer >= 3f)
        {
            this.timer = 0f;
            if (await this.pinger.PingHostAsync()) { this.failedPingCount = 0; }
            else { this.failedPingCount++; }
        }

        // If three pings have failed in succession, activate the connectivity warning message
        if (failedPingCount >= 3) { if (!this.textObject.gameObject.activeSelf) this.textObject.gameObject.SetActive(true); }
        else { if (this.textObject.gameObject.activeSelf) this.textObject.gameObject.SetActive(false);}

        // If the connectivity warning message is displayed, have it slowly flash to draw attention
        if (this.textObject.gameObject.activeSelf)
        {
            if (this.decreasing) this.textObject.color = new Color(this.textObject.color.r, this.textObject.color.g, this.textObject.color.b, this.textObject.color.a - 3f/255f);
            else this.textObject.color = new Color(this.textObject.color.r, this.textObject.color.g, this.textObject.color.b, this.textObject.color.a + 3f/255f);

            if (this.textObject.color.a < 150f/255f) this.decreasing = false;
            if (this.textObject.color.a >= 1f) this.decreasing = true;
        }
    }

}