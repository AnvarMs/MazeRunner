
using UnityEngine;
public enum Direction {
    Left,
    Right,
    Frond,
    Back
}
public class MazeCell : MonoBehaviour
{
    [SerializeField]
    private GameObject _leftWall, _rightWall, _frondWall, _backWall, _unvisitedWall;

    public bool IsVisited { get; private set; } = false;


    public void OnVisit()
    {
        IsVisited = true;
        _unvisitedWall.SetActive(false);
    }

    public void DeactivateWall(Direction direction)
    {
        GameObject wall = null;
        switch (direction)
        {
            case Direction.Left:
                wall = _leftWall;
                break;
            case Direction.Right:
                wall = _rightWall;
                break;
            case Direction.Frond:
                wall = _frondWall;
                break;
            case Direction.Back:
                wall = _backWall;
                break;
        }


        if (wall != null) wall.SetActive(false);
    }
    public bool HasWall(Direction direction)
{
    switch (direction)
    {
        case Direction.Left: return _leftWall.activeSelf;
        case Direction.Right: return _rightWall.activeSelf;
        case Direction.Frond: return _frondWall.activeSelf;
        case Direction.Back: return _backWall.activeSelf;
    }
    return false;
}

}
