using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    //手机测试文本
    public Text txt;
    
    //竖向滑动的滚动页————集合
    public List<ScrollRect> scrollRects;

    //横向滑动的滚动页
    private ScrollRect _scrollRect;
    
    //横向滚动页码索引集合
    private readonly List<float> _pageArray = new List<float>();
    //手指抬起页码回归的速度
    private const float Smoothing = 8; //滑动的速度
    
    //是否结束了滑动
    private bool _isEndDrag;
    //是否需要检测是上下滑还是左右滑（当一个方向确定下来时，这时就只需要在该方向滑动，将不再检测上滑还是左滑）
    private bool _isNeedCheck;
    //是否检查页码回归（如果是上下滑动时，则不需要判定页码回归）
    private bool _isCheckPage;
    
    //页码索引
    private int _horizontalIndex;
    //抬起手指横向页码应该回归的目标值
    private float _targetHorizontalPosition;
    //滑动手指横向间距
    private float _horizontalSpace;
    //滑动手指竖向间距
    private float _verticalSpace;
    //横向滑动左边界
    private float _leftBorder;
    //横向滑动右边界
    private float _rightBorder;
    
    private void Start()
    {
        _scrollRect = GetComponent<ScrollRect>();
        
        _pageArray.Clear();
        for (int i = 0; i < scrollRects.Count; i++) 
        {
            _pageArray.Add(i / ((scrollRects.Count - 1) * 1.0f));
            //Debug.Log($"index:{i},val:{i / ((scrollRects.Count - 1) * 1.0f)}");
        }

        float space = (1 * 1.0f / (scrollRects.Count - 1)) / 2;
        _leftBorder = -space;
        _rightBorder = 1 + space;
        //Debug.Log($"left:{_leftBorder},right:{_rightBorder}");
    }

    /// <summary>
    /// 判定是否启用竖向滑动的检测
    /// </summary>
    /// <param name="isCan"></param>
    private void SetVertical(bool isCan)
    {
        foreach (var t in scrollRects)
        {
            t.enabled = isCan;
        }
    }
    
    private void Update()
    {
        //横向滑动结束时，页码回归
        if (_isEndDrag)
        {
            _scrollRect.horizontalNormalizedPosition = Mathf.Lerp(_scrollRect.horizontalNormalizedPosition,
                _targetHorizontalPosition, Time.deltaTime * Smoothing); //当前值  目标值  速度  （在两值间渐进）
            if (Mathf.Abs(_scrollRect.horizontalNormalizedPosition - _targetHorizontalPosition) <= 0.001f)
            {
                //缓动结束
                _isEndDrag = false;
                _scrollRect.horizontalNormalizedPosition = _targetHorizontalPosition;
                SetVertical(true);
            }
        }
        
        //如果页码正在回归的缓动中，则不触发手指滑动事件
        if (_isEndDrag) return;
        
        //手机触摸屏幕，开始检测
        if (Input.touchCount > 0)
        {
            Touch myTouch = Input.touches[0];
            
            //拿到手指点下那一时刻的位置
            Vector2 clickPos = myTouch.rawPosition;
            //拿到手指点下实时的位置
            Vector2 pos = myTouch.position;

            //在手机上进行屏幕上进行显示
            txt.text = "raw:" + clickPos + "pos:" + pos;

            //开始检测手指的状态
            switch (myTouch.phase)
            {
                //手指按下时
                case TouchPhase.Began:
                    _isEndDrag = false;

                    _isNeedCheck = true;
                    
                    //拿到手指按下时，当前页码处于哪一页
                    for (int i = 0; i < _pageArray.Count; i++)
                    {
                        if (Mathf.Abs(_scrollRect.horizontalNormalizedPosition - _pageArray[i]) <= 0.001f)
                        {
                            _horizontalIndex = i;
                            //Debug.Log($"click get index{_horizontalIndex}");
                            break;
                        }
                    }
                    break;
                //手指滑动时
                case TouchPhase.Moved:
                    _isEndDrag = false;
                    
                    float temp = (pos.x - clickPos.x) * 1.0f / 1000;

                    //判断是否需要检测，如果不需要则说明手指已经检测出是向左右滑动还是向上下滑动
                    if (_isNeedCheck)
                    {
                        _horizontalSpace = Mathf.Abs(pos.x - clickPos.x);
                        _verticalSpace = Mathf.Abs(pos.y - clickPos.y);
                    }
                    
                    //比较手指横向滑动的距离与竖向滑动的距离用来判断是横向滑动还是竖向滑动
                    if (_horizontalSpace > _verticalSpace)
                    {
                        //横向滑动，则标记为不再需要检测，只进行横向滑动的逻辑
                        _isNeedCheck = false;
                        SetVertical(false);
                        _scrollRect.horizontalNormalizedPosition = -temp + _pageArray[_horizontalIndex];
                        if (_scrollRect.horizontalNormalizedPosition <= _leftBorder)
                        {
                            _scrollRect.horizontalNormalizedPosition = _leftBorder;
                        }

                        if (_scrollRect.horizontalNormalizedPosition >= _rightBorder)
                        {
                            _scrollRect.horizontalNormalizedPosition = _rightBorder;
                        }

                        //标记滑动结束需要页码回归
                        _isCheckPage = true;
                    }
                    else
                    {
                        //竖向滑动，则标记为不再需要检测，只进行竖向滑动的逻辑
                        _isNeedCheck = false;
                        SetVertical(true);
                        
                        //标记滑动结束不需要页码回归
                        _isCheckPage = false;
                    }
                    
                    break;
                case TouchPhase.Stationary:
                    break;
                //滑动结束时
                case TouchPhase.Ended:
                    //判断是否需要页码回归，只有横向滑动时才需要页码回归
                    if (_isCheckPage)
                    {
                        float tempVal = pos.x - clickPos.x;
                        //判断手指抬起时和按下时间距超过50则直接回归到下一页，否则回归到本页
                        if (Mathf.Abs(tempVal) >= 50)
                        {
                            int newIndex;
                            if (tempVal >= 0)
                            {
                                //往左滑
                                newIndex= _horizontalIndex - 1;
                                _horizontalIndex = newIndex < 0 ? 0 : newIndex;
                            }
                            else
                            {
                                //往右滑
                                newIndex = _horizontalIndex + 1;
                                _horizontalIndex = newIndex >= _pageArray.Count ? _pageArray.Count - 1 : newIndex;
                            }
                        }

                        //将回归的页码值赋给目标值
                        _targetHorizontalPosition = _pageArray[_horizontalIndex];

                        //开始页码回归缓动效果
                        _isEndDrag = true;
                    }
                    break;
                case TouchPhase.Canceled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
