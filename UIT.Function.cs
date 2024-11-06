using System;

namespace ComAssist
{
    public partial class UIT
    {
        public UIT()
        {
            this.i = 0;
        }

        //
        public Byte ConvertInt32ToByte(Int32 meDat)
        {
            this.i = meDat;
            return this.b0;
        }

        //
        public Byte ConvertFloatToByte(float meDat)
        {
            this.f = meDat;
            return this.b0;
        }

        //
        public float ConvertInt32ToFloat(Int32 meDat)
        {
            this.i = meDat;
            return this.f;
        }

        //
        public Int32 ConvertFloatToInt32(float meDat)
        {
            this.f = meDat;
            return this.i;
        }
    }
}

//end

