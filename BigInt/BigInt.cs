using System.Numerics;

namespace BigInt;

public struct BigInt : IAdditionOperators<BigInt, BigInt, BigInt>, ISubtractionOperators<BigInt, BigInt, BigInt>,
                       IMultiplyOperators<BigInt, BigInt, BigInt>, IComparisonOperators<BigInt, BigInt, bool>
{
    private bool _isPositive;
    private List<byte> _bytes;

    public BigInt() : this(true, [0]) {}
    /// <summary>
    /// </summary>
    /// <param name="isPositive"></param>
    /// <param name="bytes">List of bytes representing the number in little-endian order</param>
    public BigInt(bool isPositive, List<byte> bytes)
    {
        _isPositive = isPositive;
        _bytes = bytes;
    }

    #region arithmetic operators
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
    
    // not very efficient algorithm, needs to be rewritten
    public static BigInt operator *(BigInt left, BigInt right)
    {
        // return 0 if one of numbers is 0
        if (left == 0 || right == 0)
            return new BigInt(true, [0]);

        // if one of the numbers is 1, return the other one
        // bytes need to be copied
        if (left == 1)
        {
            byte[] bytesClone = new byte[right._bytes.Count];
            right._bytes.CopyTo(bytesClone);
            return new BigInt(left._isPositive, [.. bytesClone]);
        }

        if (right == 1)
        {
            byte[] bytesClone = new byte[left._bytes.Count];
            left._bytes.CopyTo(bytesClone);
            return new BigInt(left._isPositive, [.. bytesClone]);
        }

        // long multiplication
        var result = new BigInt(true, [0]);
        for (int i = 0; i < left._bytes.Count; i++)
        {
            // create trailing 0s
            var bytes = Enumerable.Repeat((byte)0, i).ToList();

            // calculate partial result of multiplication
            var carry = 0;
            for (int j = 0; j < right._bytes.Count; j++)
            {
                var multResult = left._bytes[i] * right._bytes[j] + carry;

                if (multResult > 255)
                {
                    carry = multResult / 256;
                    multResult %= 256;
                }
                else
                    carry = 0;

                bytes.Add((byte)multResult);
            }

            if (carry != 0)
                bytes.Add((byte)carry);

            result += new BigInt(true, bytes);
        }

        // determine sign
        result._isPositive = left._isPositive == right._isPositive;
        return result;
    }
    #endregion

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

        // determine the sign
        var sign = value > 0;
        value = Math.Abs(value);

        // fill list of bytes
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
    #endregion
}