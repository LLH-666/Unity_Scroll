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
    
    //滑动方向类型
    private enum slideVector { nullVector, left, right };
    //当前滑动方向
    private slideVector _currentVector = slideVector.nullVector;

    //手指抬起页码回归的速度(值越大越快)
    private const float PageBackSpeed = 0.01f;
    //横向滑动的速度(值越大滑动越缓慢)
    private const float HorizontalSpeed = 50;
    //触发检测阀值
    private const float InoutThreshold = 50;
    //滑动判断的时间间隔
    private const float offsetTime = 0.01f;

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
    //横向滑动条间隔
    private float _boarderSpace;
    //页码回归计时器
    private float _pageBackTime;
    //滑动时间计数器
    private float _moveTimer;

    //拿到手指点下那一时刻的位置
    private Vector2 clickPos;
    //拿到手指点下的上一个位置
    private Vector2 lastPos;

    private void Start()
    {
        _scrollRect = GetComponent<ScrollRect>();

        _pageArray.Clear();
        for (int i = 0; i < scrollRects.Count; i++)
        {
            _pageArray.Add(i / ((scrollRects.Count - 1) * 1.0f));
            //Debug.Log($"index:{i},val:{i / ((scrollRects.Count - 1) * 1.0f)}");
        }

        _boarderSpace = (1 * 1.0f / (scrollRects.Count - 1)) / 2;
    }

    private void Update()
    {
        //Debug.LogError(_scrollRect.horizontalNormalizedPosition);
        
        //横向滑动结束时，页码回归
        if (_isEndDrag)
        {
            _pageBackTime += PageBackSpeed;
            _pageBackTime = Mathf.Clamp(_pageBackTime, 0, 1);
            _scrollRect.horizontalNormalizedPosition = Mathf.Lerp(_scrollRect.horizontalNormalizedPosition,
                _targetHorizontalPosition, _pageBackTime); //当前值  目标值  速度  （在两值间渐进）
            if (Mathf.Abs(_scrollRect.horizontalNormalizedPosition - _targetHorizontalPosition) <= 0.001f)
            {
                //缓动结束
                _pageBackTime = 0;
                _isEndDrag = false;
                _scrollRect.horizontalNormalizedPosition = _targetHorizontalPosition;
                SetVertical(true);
            }
        }

        //手机触摸屏幕，开始检测
        if (Input.touchCount > 0)
        {
            Touch myTouch = Input.touches[0];

            //在手机上进行屏幕上进行显示
            txt.text = "raw:" + clickPos + "pos:" + myTouch.position;

            //开始检测手指的状态
            switch (myTouch.phase)
            {
                //手指按下时
                case TouchPhase.Began:
                    HandleBegan(myTouch);
                    break;
                //手指滑动时
                case TouchPhase.Moved:
                    HandleMove(myTouch);
                    break;
                //滑动结束时
                case TouchPhase.Ended:
                    HandleEnd(myTouch);
                    break;
            }
        }
    }

    private void HandleBegan(Touch myTouch)
    {
        _isEndDrag = false;
        _pageBackTime = 0;
        _moveTimer = 0;

        _isNeedCheck = true;

        clickPos = myTouch.position;
        lastPos = myTouch.position;

        _horizontalSpace = 0;
        _verticalSpace = 0;
        
        //拿到手指按下时，当前页码处于哪一页
        _horizontalIndex = GetPageIndex(_scrollRect.horizontalNormalizedPosition);
        //Debug.LogError($"_horizontalIndex:{_horizontalIndex}");
    }

    private void HandleMove(Touch myTouch)
    {
        _isEndDrag = false;

        var currentPos = myTouch.position;
        _moveTimer += Time.deltaTime;
        if (_moveTimer > offsetTime)
        {
            if (currentPos.x < lastPos.x)
            {
                _currentVector = slideVector.left;
                //Debug.Log("Turn left");
            }
            if (currentPos.x > lastPos.x)
            {
                _currentVector = slideVector.right;
                //Debug.Log("Turn right");
            }
            lastPos = currentPos;
            _moveTimer = 0;
        }

        //判断是否需要检测，如果不需要则说明手指已经检测出是向左右滑动还是向上下滑动
        if (_isNeedCheck)
        {
            _horizontalSpace = Mathf.Abs(currentPos.x - clickPos.x);
            _verticalSpace = Mathf.Abs(currentPos.y - clickPos.y);
        }

        //实时横向滑动
        if (_horizontalSpace >= InoutThreshold || _verticalSpace >= InoutThreshold)
        {
            //比较手指横向滑动的距离与竖向滑动的距离用来判断是横向滑动还是竖向滑动
            if (_horizontalSpace > _verticalSpace)
            {
                SetVertical(false);

                var tempValue = _boarderSpace / HorizontalSpeed;
                switch (_currentVector)
                {
                    case slideVector.nullVector:
                        break;
                    case slideVector.left:
                        _scrollRect.horizontalNormalizedPosition += tempValue;
                        break;
                    case slideVector.right:
                        _scrollRect.horizontalNormalizedPosition -= tempValue;
                        break;
                }

                //标记滑动结束需要页码回归
                _isCheckPage = true;
            }
            else
            {
                SetVertical(true);

                //标记滑动结束不需要页码回归
                _isCheckPage = false;
            }

            //判断成功,则标记为不再需要检测
            _isNeedCheck = false;
        }
    }

    private void HandleEnd(Touch myTouch)
    {
        _currentVector = slideVector.nullVector;
        
        //判断是否需要页码回归，只有横向滑动时才需要页码回归
        if (_isCheckPage)
        {
            float tempVal = myTouch.position.x - clickPos.x;
            //判断手指抬起时和按下时 间距超过50则直接回归到下一页，否则回归到本页
            if (Mathf.Abs(tempVal) >= 50)
            {
                int newIndex;
                if (tempVal >= 0)
                {
                    //往左滑
                    newIndex = _horizontalIndex - 1;
                    _horizontalIndex = newIndex < 0 ? 0 : newIndex;
                }
                else
                {
                    //往右滑
                    newIndex = _horizontalIndex + 1;
                    _horizontalIndex = newIndex >= _pageArray.Count ? _pageArray.Count - 1 : newIndex;
                }
            }
        }
        
        //根据滑动条的位置判断是否回归到本页
        //_horizontalIndex = GetPageIndex(_scrollRect.horizontalNormalizedPosition);

        //将回归的页码值赋给目标值
        _targetHorizontalPosition = _pageArray[_horizontalIndex];

        //开始页码回归缓动效果
        _isEndDrag = true;
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

    private int GetPageIndex(float horizontalPos)
    {
        if (horizontalPos < 0)
        {
            return 0;
        }

        if (horizontalPos > 1)  
        {
            return _pageArray.Count - 1;
        }
        
        int pageIndex = 0;

        for (int i = 0; i < _pageArray.Count; i++)
        {
            var item = _pageArray[i];
            var left = item - _boarderSpace;
            var right = item + _boarderSpace;
            
            if (horizontalPos >= left && horizontalPos <= right)
            {
                pageIndex = i;
            }
        }

        return pageIndex;
    }
}