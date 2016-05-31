/**************************
 * 文件名:InfinityGridLayoutGroup.cs;
 * 文件描述:无限滚动GridLayoutGroup,动态创建滚动Item;
 * 实现无限滚动，需要的最少的child数量。屏幕上能看到的+一行看不到的，比如我在屏幕上能看到 2 行，每一行 2 个。则这个值为 2行*2个 + 1 行* 2个 = 6个。
 * 创建日期:2016/05/31;
 * Author:ThisisGame;
 * Page:https://github.com/ThisisGame/InfinityGridLayoutGroup
 ***************************/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(GridLayoutGroup))]
[RequireComponent(typeof(ContentSizeFitter))]
public class InfinityGridLayoutGroup : MonoBehaviour 
{

    [SerializeField]
    int minAmount = 0;//实现无限滚动，需要的最少的child数量。屏幕上能看到的+一行看不到的，比如我在屏幕上能看到 2 行，每一行 2 个。则这个值为 2行*2个 + 1 行* 2个 = 6个。

    RectTransform rectTransform;

    GridLayoutGroup gridLayoutGroup;
    ContentSizeFitter contentSizeFitter;

    ScrollRect scrollRect;

    List<RectTransform> children=new List<RectTransform>();

    Vector2 startPosition;

    int amount = 0;

    public delegate void UpdateChildrenCallbackDelegate(int index, Transform trans);
    public UpdateChildrenCallbackDelegate updateChildrenCallback = null;

    int realIndex = -1;
    int realIndexUp = -1; //从下往上;

    bool hasInit = false;
    Vector2 gridLayoutSize;
    Vector2 gridLayoutPos;
    Dictionary<Transform, Vector2> childsAnchoredPosition = new Dictionary<Transform, Vector2>();
    Dictionary<Transform, int> childsSiblingIndex = new Dictionary<Transform, int>();


	// Use this for initialization
	void Start ()
    {
        //StartCoroutine(InitChildren());
	}

    IEnumerator InitChildren()
    {
        yield return 0;

        if (!hasInit)
        {
            //获取Grid的宽度;
            rectTransform = GetComponent<RectTransform>();

            gridLayoutGroup = GetComponent<GridLayoutGroup>();
            gridLayoutGroup.enabled = false;
            contentSizeFitter = GetComponent<ContentSizeFitter>();
            contentSizeFitter.enabled = false;

            gridLayoutPos = rectTransform.anchoredPosition;
            gridLayoutSize = rectTransform.sizeDelta;

            
            //注册ScrollRect滚动回调;
            scrollRect = transform.parent.GetComponent<ScrollRect>();
            scrollRect.onValueChanged.AddListener((data) => { ScrollCallback(data); });

            //获取所有child anchoredPosition 以及 SiblingIndex;
            for (int index = 0; index < transform.childCount; index++)
            {
                Transform child=transform.GetChild(index);
                RectTransform childRectTrans= child.GetComponent<RectTransform>();
                childsAnchoredPosition.Add(child, childRectTrans.anchoredPosition);

                childsSiblingIndex.Add(child, child.GetSiblingIndex());
            }
        }
        else
        {
            rectTransform.anchoredPosition = gridLayoutPos;
            rectTransform.sizeDelta = gridLayoutSize;

            children.Clear();

            realIndex = -1;
            realIndexUp = -1;

            //children重新设置上下顺序;
            foreach (var info in childsSiblingIndex)
            {
                info.Key.SetSiblingIndex(info.Value);
            }

            //children重新设置anchoredPosition;
            for (int index = 0; index < transform.childCount; index++)
            {
                Transform child = transform.GetChild(index);
                
                RectTransform childRectTrans = child.GetComponent<RectTransform>();
                if (childsAnchoredPosition.ContainsKey(child))
                {
                    childRectTrans.anchoredPosition = childsAnchoredPosition[child];
                }
                else
                {
                    Debug.LogError("childsAnchoredPosition no contain "+child.name);
                }
            }
        }

        //获取所有child;
        for (int index = 0; index < transform.childCount; index++)
        {
            Transform trans = transform.GetChild(index);
            trans.gameObject.SetActive(true);

            children.Add(transform.GetChild(index).GetComponent<RectTransform>());

            //初始化前面几个;
            UpdateChildrenCallback(children.Count - 1, transform.GetChild(index));
        }

        startPosition = rectTransform.anchoredPosition;

        realIndex = children.Count - 1;

        //Debug.Log( scrollRect.transform.TransformPoint(Vector3.zero));

       // Debug.Log(transform.TransformPoint(children[0].localPosition));

        hasInit = true;

        //如果需要显示的个数小于设定的个数;
        for (int index = 0; index < minAmount; index++)
        {
            children[index].gameObject.SetActive(index < amount);
        }

        if (gridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            //如果小了一行，则需要把GridLayout的高度减去一行的高度;
            int row = (minAmount - amount) / gridLayoutGroup.constraintCount;
            if (row > 0)
            {
                rectTransform.sizeDelta -= new Vector2(0, (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y) * row);
            }
        }
        else
        {
            //如果小了一列，则需要把GridLayout的宽度减去一列的宽度;
            int column = (minAmount - amount) / gridLayoutGroup.constraintCount;
            if (column > 0)
            {
                rectTransform.sizeDelta -= new Vector2((gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x) * column, 0);
            }
        }
    }
	
	// Update is called once per frame
	void Update () 
    {
	
	}


    void ScrollCallback(Vector2 data)
    {
        UpdateChildren();
    }

    void UpdateChildren()
    {
        if (transform.childCount < minAmount)
        {
            return;
        }

        Vector2 currentPos = rectTransform.anchoredPosition;

        if (gridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            float offsetY = currentPos.y - startPosition.y;

            if (offsetY > 0)
            {
                //向上拉，向下扩展;
                {
                    if (realIndex >= amount - 1)
                    {
                        startPosition = currentPos;
                        return;
                    }

                    float scrollRectUp = scrollRect.transform.TransformPoint(Vector3.zero).y;

                    Vector3 childBottomLeft = new Vector3(children[0].anchoredPosition.x, children[0].anchoredPosition.y - gridLayoutGroup.cellSize.y, 0f);
                    float childBottom = transform.TransformPoint(childBottomLeft).y;

                    if (childBottom >= scrollRectUp)
                    {
                        //Debug.Log("childBottom >= scrollRectUp");

                        //移动到底部;
                        for (int index = 0; index < gridLayoutGroup.constraintCount; index++)
                        {
                            children[index].SetAsLastSibling();

                            children[index].anchoredPosition = new Vector2(children[index].anchoredPosition.x, children[children.Count - 1].anchoredPosition.y - gridLayoutGroup.cellSize.y - gridLayoutGroup.spacing.y);

                            realIndex++;

                            if (realIndex > amount - 1)
                            {
                                children[index].gameObject.SetActive(false);
                            }
                            else
                            {
                                UpdateChildrenCallback(realIndex, children[index]);
                            }
                        }

                        //GridLayoutGroup 底部加长;
                        rectTransform.sizeDelta += new Vector2(0, gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y);

                        //更新child;
                        for (int index = 0; index < children.Count; index++)
                        {
                            children[index] = transform.GetChild(index).GetComponent<RectTransform>();
                        }
                    }
                }
            }
            else
            {
                //Debug.Log("Drag Down");
                //向下拉，下面收缩;
                if (realIndex + 1 <= children.Count)
                {
                    startPosition = currentPos;
                    return;
                }
                RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
                Vector3 scrollRectAnchorBottom = new Vector3(0, -scrollRectTransform.rect.height - gridLayoutGroup.spacing.y, 0f);
                float scrollRectBottom = scrollRect.transform.TransformPoint(scrollRectAnchorBottom).y;

                Vector3 childUpLeft = new Vector3(children[children.Count - 1].anchoredPosition.x, children[children.Count - 1].anchoredPosition.y, 0f);

                float childUp = transform.TransformPoint(childUpLeft).y;

                if (childUp < scrollRectBottom)
                {
                    //Debug.Log("childUp < scrollRectBottom");

                    //把底部的一行 移动到顶部
                    for (int index = 0; index < gridLayoutGroup.constraintCount; index++)
                    {
                        children[children.Count - 1 - index].SetAsFirstSibling();

                        children[children.Count - 1 - index].anchoredPosition = new Vector2(children[children.Count - 1 - index].anchoredPosition.x, children[0].anchoredPosition.y + gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y);

                        children[children.Count - 1 - index].gameObject.SetActive(true);

                        UpdateChildrenCallback(realIndex - children.Count - index, children[children.Count - 1 - index]);
                    }

                    realIndex -= gridLayoutGroup.constraintCount;

                    //GridLayoutGroup 底部缩短;
                    rectTransform.sizeDelta -= new Vector2(0, gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y);

                    //更新child;
                    for (int index = 0; index < children.Count; index++)
                    {
                        children[index] = transform.GetChild(index).GetComponent<RectTransform>();
                    }
                }
            }
        }
        else
        {
            float offsetX = currentPos.x - startPosition.x;

            if (offsetX < 0)
            {
                //向左拉，向右扩展;
                {
                    if (realIndex >= amount - 1)
                    {
                        startPosition = currentPos;
                        return;
                    }

                    float scrollRectLeft = scrollRect.transform.TransformPoint(Vector3.zero).x;

                    Vector3 childBottomRight = new Vector3(children[0].anchoredPosition.x+ gridLayoutGroup.cellSize.x, children[0].anchoredPosition.y, 0f);
                    float childRight = transform.TransformPoint(childBottomRight).x;

                   // Debug.LogError("childRight=" + childRight);

                    if (childRight <= scrollRectLeft)
                    {
                        //Debug.Log("childRight <= scrollRectLeft");

                        //移动到右边;
                        for (int index = 0; index < gridLayoutGroup.constraintCount; index++)
                        {
                            children[index].SetAsLastSibling();

                            children[index].anchoredPosition = new Vector2(children[children.Count - 1].anchoredPosition.x + gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x, children[index].anchoredPosition.y);

                            realIndex++;

                            if (realIndex > amount - 1)
                            {
                                children[index].gameObject.SetActive(false);
                            }
                            else
                            {
                                UpdateChildrenCallback(realIndex, children[index]);
                            }
                        }

                        //GridLayoutGroup 右侧加长;
                        rectTransform.sizeDelta += new Vector2(gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x,0);

                        //更新child;
                        for (int index = 0; index < children.Count; index++)
                        {
                            children[index] = transform.GetChild(index).GetComponent<RectTransform>();
                        }
                    }
                }
            }
            else
            {
                //Debug.Log("Drag Down");
                //向右拉，右边收缩;
                if (realIndex + 1 <= children.Count)
                {
                    startPosition = currentPos;
                    return;
                }
                RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
                Vector3 scrollRectAnchorRight = new Vector3(scrollRectTransform.rect.width + gridLayoutGroup.spacing.x, 0, 0f);
                float scrollRectRight = scrollRect.transform.TransformPoint(scrollRectAnchorRight).x;

                Vector3 childUpLeft = new Vector3(children[children.Count - 1].anchoredPosition.x, children[children.Count - 1].anchoredPosition.y, 0f);

                float childLeft = transform.TransformPoint(childUpLeft).x;

                if (childLeft >= scrollRectRight)
                {
                    //Debug.LogError("childLeft > scrollRectRight");

                    //把右边的一行 移动到左边;
                    for (int index = 0; index < gridLayoutGroup.constraintCount; index++)
                    {
                        children[children.Count - 1 - index].SetAsFirstSibling();

                        children[children.Count - 1 - index].anchoredPosition = new Vector2(children[0].anchoredPosition.x - gridLayoutGroup.cellSize.x - gridLayoutGroup.spacing.x,children[children.Count - 1 - index].anchoredPosition.y);

                        children[children.Count - 1 - index].gameObject.SetActive(true);

                        UpdateChildrenCallback(realIndex - children.Count - index, children[children.Count - 1 - index]);
                    }

                    

                    //GridLayoutGroup 右侧缩短;
                    rectTransform.sizeDelta -= new Vector2(gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x, 0);

                    //更新child;
                    for (int index = 0; index < children.Count; index++)
                    {
                        children[index] = transform.GetChild(index).GetComponent<RectTransform>();
                    }

                    realIndex -= gridLayoutGroup.constraintCount;
                }
            }
        }

        startPosition = currentPos;
    }

    void UpdateChildrenCallback(int index,Transform trans)
    {
        if (updateChildrenCallback != null)
        {
            updateChildrenCallback(index, trans);
        }
    }


    /// <summary>
    /// 设置总的个数;
    /// </summary>
    /// <param name="count"></param>
    public void SetAmount(int count)
    {
        amount = count;

        StartCoroutine(InitChildren());
    }
}
