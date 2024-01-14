using System.Numerics;

namespace BigInt;

public struct BigInt : IAdditionOperators<BigInt, BigInt, BigInt>, ISubtractionOperators<BigInt, BigInt, BigInt>,
                       IComparisonOperators<BigInt, BigInt, bool>
{
    private bool _isPositive;
    private List<byte> _bytes;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="isPositive"></param>
    /// <param name="bytes">List of bytes representing the number in little-endian order</param>
    public BigInt(bool isPositive, List<byte> bytes)
    {
        _isPositive = isPositive;
        _bytes = bytes;
    }

    public static BigInt operator +(BigInt left, BigInt right)
    {
        // if signs are not matching, it's subtraction
        if (left._isPositive && !right._isPositive)
            return left - new BigInt(true, right._bytes);
        else if (!left._isPositive && right._isPositive)
            return right - new BigInt(true, left._bytes);

        // switch numbers, if the right number uses more bytes
        if (left._bytes.Count < right._bytes.Count)
            (left, right) = (right, left);

        var newBytes = new List<byte>();

        // calulate new byte for each byte of the right side
        var carry = false;
        for (int i = 0; i < right._bytes.Count; i++)
        {
            var sum = left._bytes[i] + right._bytes[i];

            if (carry)
            {
                carry = false;
                sum++;
            }

            if (sum > 255)
            {
                carry = true;
                sum %= 256;
            }

            newBytes.Add((byte)sum);
        }

        // if the number on the left has more bytes, just copy them to the new list
        for (int i = right._bytes.Count; i < left._bytes.Count; i++)
        {
            var num = (int)left._bytes[i];

            if (carry)
            {
                carry = false;
                num++;
            }

            if (num > 255)
            {
                carry = true;
                num %= 256;
            }

            newBytes.Add((byte)num);
        }

        if (carry)
            newBytes.Add(1);

        return new BigInt(true, newBytes);
    }

    public static BigInt operator -(BigInt left, BigInt right)
    {
        // a - (-b) <=> a + b
        if (!right._isPositive)
            return left + new BigInt(true, right._bytes);

        // -a - b <=> -(a + b)
        if (!left._isPositive)
        {
            var result = new BigInt(true, left._bytes) + right;
            result._isPositive = false;
            return result;
        }

        // a < b => a - b <=> -b + a <=> -(b - a)
        // makes it easier to implement the subtraction
        if (left._bytes.Count < right._bytes.Count)
            return new BigInt(false, (right - left)._bytes);

        // subtract all the bytes on the right side
        var newBytes = new List<byte>();
        var borrow = 0;
        for (int i = 0; i < right._bytes.Count; i++)
        {
            var result = left._bytes[i] - right._bytes[i] - borrow;
            //result = (result + 256) % 256;

            if (result < 0)
            {
                borrow = 1;
                result += 256;
            }
            else
                borrow = 0;

            newBytes.Add((byte)result);
        }

        // copy remaining bytes on the left side
        for (int i = right._bytes.Count; i < left._bytes.Count; i++)
        {
            var result = left._bytes[i] - borrow;

            if (result < 0)
            {
                borrow = 1;
                result += 256;
            }
            else
                borrow = 0;

            newBytes.Add((byte)result);
        }

        // sometimes, the most significant bytes remain 0, remove them
        for (int i = newBytes.Count - 1; i >= 0; i--)
        {
            if (newBytes[i] == 0)
                newBytes.RemoveAt(i);
            else
                break;
        }

        return new BigInt(true, newBytes);
    }

    // not very readable code, refactoring is needed
    #region comparison operators
    public static bool operator >(BigInt left, BigInt right)
    {
        if (left._isPositive != right._isPositive)
            return left._isPositive && !right._isPositive;

        if (left._bytes.Count != right._bytes.Count)
            return (left._bytes.Count > right._bytes.Count) == left._isPositive;

        for (int i = left._bytes.Count - 1; i >= 0; i--)
        {
            if (left._bytes[i] != right._bytes[i])
                return (left._bytes[i] > right._bytes[i]) == left._isPositive;
        }

        return false;
    }

    public static bool operator >=(BigInt left, BigInt right)
    {
        if (left._isPositive != right._isPositive)
            return left._isPositive && !right._isPositive;

        if (left._bytes.Count != right._bytes.Count)
            return (left._bytes.Count > right._bytes.Count) == left._isPositive;

        for (int i = left._bytes.Count - 1; i >= 0; i--)
        {
            if (left._bytes[i] != right._bytes[i])
                return (left._bytes[i] > right._bytes[i]) == left._isPositive;
        }

        return true;
    }

    public static bool operator <(BigInt left, BigInt right) => right > left;

    public static bool operator <=(BigInt left, BigInt right) => right >= left;

    public static bool operator ==(BigInt left, BigInt right)
    {
        if (left._isPositive != right._isPositive || left._bytes.Count != right._bytes.Count)
            return false;

        for (int i = 0; i < left._bytes.Count; i++)
        {
            if (left._bytes[i] != right._bytes[i])
                return false;
        }

        return true;
    }

    public static bool operator !=(BigInt left, BigInt right) => !(left == right);
    #endregion

    #region implicit operators
    public static implicit operator BigInt(int value)
    {
        if (value == 0)
            return new BigInt(true, [0]);

        var sign = value > 0;
        value = Math.Abs(value);

        var bytes = new List<byte>();
        var carry = value;
        while (carry > 0)
        {
            var num = carry % 256;
            carry /= 256;
            bytes.Add((byte)num);
        }

        return new BigInt(sign, bytes);
    }

    private static BigInt FromIntegralType<T>(T value)
    {

    }
    #endregion
}