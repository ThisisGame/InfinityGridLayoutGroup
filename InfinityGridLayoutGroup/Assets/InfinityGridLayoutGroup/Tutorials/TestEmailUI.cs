using UnityEngine;
using System.Collections;
using UnityEngine.UI;


namespace ThisisGame
{

    public class TestEmailUI : MonoBehaviour
    {

        InfinityGridLayoutGroup infinityGridLayoutGroup;

        int amount = 20;

        // Use this for initialization
        void Start()
        {
            ////初始化数据列表;
            infinityGridLayoutGroup = transform.Find("Panel_Scroll/Panel_Grid").GetComponent<InfinityGridLayoutGroup>();
            infinityGridLayoutGroup.SetAmount(amount);
            infinityGridLayoutGroup.updateChildrenCallback = UpdateChildrenCallback;
        }


        void OnGUI()
        {
            if (GUILayout.Button("Add one item"))
            {
                infinityGridLayoutGroup.SetAmount(++amount);
            }
            if (GUILayout.Button("remove one item"))
            {
                infinityGridLayoutGroup.SetAmount(--amount);
            }
        }

        void UpdateChildrenCallback(int index, Transform trans)
        {
            Debug.Log("UpdateChildrenCallback: index=" + index + " name:" + trans.name);

            Text text = trans.Find("Text").GetComponent<Text>();
            text.text = index.ToString();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
