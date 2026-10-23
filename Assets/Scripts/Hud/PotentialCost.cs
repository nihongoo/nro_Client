using System;
using System.Globalization;

namespace Hud
{
    internal static class PotentialCost
    {
        internal const int CriticalTiers=14;
        internal static long Step(int type) => type<=1?20L:1L;
        internal static long Calculate(int type,long current,int steps)
        {
            if(type<0||type>4||current<0||steps<=0)throw new ArgumentException();
            checked {
                if(type==4) {
                    if(current>=CriticalTiers||steps>CriticalTiers-current)throw new ArgumentException();
                    long price=50000000L,cost=0;
                    for(int i=0;i<current;i++)price*=5;
                    for(int i=0;i<steps;i++){cost+=price;if(i+1<steps)price*=5;}
                    return cost;
                }
                long first=type<=1?current+1000L:type==2?current*100L:(current+5L)*100000L;
                long slope=type<=1?20L:type==2?100L:100000L;
                long a=steps,b=steps-1L;if((a&1L)==0)a/=2;else b/=2;
                return first*steps+a*b*slope;
            }
        }
        internal static bool TryCalculate(int type,long current,long target,out long cost)
        {
            cost=0;if(type<0||type>4||current<0||target<=current||target>int.MaxValue)return false;
            long delta=target-current,step=Step(type);if(delta%step!=0)return false;
            try {cost=Calculate(type,current,checked((int)(delta/step)));return true;}
            catch(OverflowException){return false;}catch(ArgumentException){return false;}
        }
        internal static long MaxTarget(int type,long current,long budget,long limit)
        {
            if(type<0||type>4||current<0||budget<0)return current;
            limit=System.Math.Min(int.MaxValue,limit);if(type==4)limit=System.Math.Min(limit,CriticalTiers);
            if(limit<=current)return current;
            long low=0,high=System.Math.Max(0,(limit-current)/Step(type));
            while(low<high) {
                long mid=low+(high-low+1)/2,target=current+mid*Step(type);
                if(TryCalculate(type,current,target,out long cost)&&cost<=budget)low=mid;else high=mid-1;
            }
            return current+low*Step(type);
        }
        internal static long Normalize(int type,long current,long requested,long maximum)
        {
            if(requested<=current)return requested;
            requested=System.Math.Min(requested,maximum);
            return current+(requested-current)/Step(type)*Step(type);
        }
        internal static long Parse(string text)
        {
            if(string.IsNullOrWhiteSpace(text))return 0;
            text=text.Replace(".","").Replace(",","").Replace(" ","");
            if(long.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out long value))return value;
            foreach(char c in text)if(c<'0'||c>'9')return 0;
            return long.MaxValue;
        }
    }
}
