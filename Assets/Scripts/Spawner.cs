using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject item;

    public float startTimeBtwItem;
    private float timeBtwItem;

    public int numOfItems;

    // Update is called once per frame
    void Update()
    {
        if (timeBtwItem <= 0 && numOfItems > 0)
        {
            Instantiate(item, transform.position, Quaternion.identity);
            timeBtwItem = startTimeBtwItem;
            numOfItems--;
        } else
        {
            timeBtwItem -= Time.deltaTime;
        }
    }
}
