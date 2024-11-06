using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace ComAssist
{
    public partial class XET
    {
        public XET()
        {
        }

        //初值:0x0000
        //多项式:0x1021
        //结果异或:0x0000
        //test ok
        public ushort AP_CRC16_XMODEN(Byte[] pData, int len, Boolean XHXL)
        {
            ushort CRC_Init_0x0000 = XMODEN_INIT;

            Byte crchi;
            Byte crclo;

            for (int i = 0; i < len; i++)
            {
                CRC_Init_0x0000 ^= (ushort)(pData[i] << 8);

                for (Byte j = 0; j < 8; j++)
                {
                    if (CRC_Init_0x0000 >= 0x8000)
                    {
                        CRC_Init_0x0000 = (ushort)((CRC_Init_0x0000 << 1) ^ 0x1021);
                    }
                    else
                    {
                        CRC_Init_0x0000 <<= 1;
                    }
                }
            }

            if (XHXL)
            {
                return CRC_Init_0x0000;
            }
            else
            {
                crchi = (Byte)(CRC_Init_0x0000 >> 8);
                crclo = (Byte)(CRC_Init_0x0000);
                return (ushort)(crclo << 8 | crchi);
            }
        }

        //初值:0xFFFF
        //多项式:0x1021
        //结果异或:0x0000
        //test ok
        public ushort AP_CRC16_CCITT_FALSE(Byte[] pData, int len, Boolean XHXL)
        {
            ushort CRC_Init_0xFFFF = CCITT_FALSE_INIT;

            Byte crchi;
            Byte crclo;

            for (int i = 0; i < len; i++)
            {
                CRC_Init_0xFFFF ^= (ushort)(pData[i] << 8);

                for (Byte j = 0; j < 8; j++)
                {
                    if (CRC_Init_0xFFFF >= 0x8000)
                    {
                        CRC_Init_0xFFFF = (ushort)((CRC_Init_0xFFFF << 1) ^ 0x1021);
                    }
                    else
                    {
                        CRC_Init_0xFFFF <<= 1;
                    }
                }
            }

            if (XHXL)
            {
                return CRC_Init_0xFFFF;
            }
            else
            {
                crchi = (Byte)(CRC_Init_0xFFFF >> 8);
                crclo = (Byte)(CRC_Init_0xFFFF);
                return (ushort)(crclo << 8 | crchi);
            }
        }

        //初值:0x0000
        //多项式:0x1021
        //结果异或:0x0000
        //test ok
        public ushort AP_CRC16_CCITT(Byte[] pData, int len, Boolean XHXL)
        {
            ushort CRC_Init_0x0000 = CCITT_INIT;

            Byte crchi = (Byte)(CRC_Init_0x0000 >> 8);
            Byte crclo = (Byte)(CRC_Init_0x0000);
            Byte index;

            for (int i = 0; i < len; i++)
            {
                index = (Byte)(crclo ^ pData[i]);
                crclo = (Byte)(crchi ^ TABLE_1021_LO[index]);
                crchi = TABLE_1021_HI[index];
            }

            if (XHXL)
            {
                return (ushort)(crchi << 8 | crclo);
            }
            else
            {
                return (ushort)(crclo << 8 | crchi);
            }
        }

        //初值:0xFFFF
        //多项式:0x1021
        //结果异或:0xFFFF
        //test ok
        public ushort AP_CRC16_X25(Byte[] pData, int len, Boolean XHXL)
        {
            ushort CRC_Init_0x0000 = X25_INIT;

            Byte crchi;
            Byte crclo;
            Byte index;

            CRC_Init_0x0000 ^= 0xFFFF;

            crchi = (Byte)(CRC_Init_0x0000 >> 8);
            crclo = (Byte)(CRC_Init_0x0000);

            for (int i = 0; i < len; i++)
            {
                index = (Byte)(crclo ^ pData[i]);
                crclo = (Byte)(crchi ^ TABLE_1021_LO[index]);
                crchi = TABLE_1021_HI[index];
            }

            if (XHXL)
            {
                return (ushort)((crchi << 8 | crclo) ^ 0xFFFF);
            }
            else
            {
                return (ushort)((crclo << 8 | crchi) ^ 0xFFFF);
            }
        }

        //初值:0x0000
        //多项式:0x8005
        //结果异或:0x0000
        //test ok
        public ushort AP_CRC16_IBM(Byte[] pData, int len, Boolean XHXL)
        {
            ushort CRC_Init_0x0000 = IBM_INIT;

            Byte crchi = (Byte)(CRC_Init_0x0000 >> 8);
            Byte crclo = (Byte)(CRC_Init_0x0000);
            Byte index;

            for (int i = 0; i < len; i++)
            {
                index = (Byte)(crclo ^ pData[i]);
                crclo = (Byte)(crchi ^ TABLE_8005_LO[index]);
                crchi = TABLE_8005_HI[index];
            }

            if (XHXL)
            {
                return (ushort)(crchi << 8 | crclo);
            }
            else
            {
                return (ushort)(crclo << 8 | crchi);
            }
        }

        //初值:0xFFFF
        //多项式:0x8005
        //结果异或:0x0000
        //test ok
        public ushort AP_CRC16_MODBUS(Byte[] pData, int len, Boolean XHXL)
        {
            ushort CRC_Init_0xFFFF = MODBUS_INIT;

            Byte crchi = (Byte)(CRC_Init_0xFFFF >> 8);
            Byte crclo = (Byte)(CRC_Init_0xFFFF);
            Byte index;

            for (int i = 0; i < len; i++)
            {
                index = (Byte)(crclo ^ pData[i]);
                crclo = (Byte)(crchi ^ TABLE_8005_LO[index]);
                crchi = TABLE_8005_HI[index];
            }

            if (XHXL)
            {
                return (ushort)(crchi << 8 | crclo);
            }
            else
            {
                return (ushort)(crclo << 8 | crchi);
            }
        }

        //初值:0x0000
        //多项式:0x8005
        //结果异或:0xFFFF
        //test ok
        public ushort AP_CRC16_MAXIM(Byte[] pData, int len, Boolean XHXL)
        {
            ushort CRC_Init_0xFFFF = MAXIM_INIT;

            Byte crchi;
            Byte crclo;
            Byte index;

            CRC_Init_0xFFFF ^= 0xFFFF;

            crchi = (Byte)(CRC_Init_0xFFFF >> 8);
            crclo = (Byte)(CRC_Init_0xFFFF);

            for (int i = 0; i < len; i++)
            {
                index = (Byte)(crclo ^ pData[i]);
                crclo = (Byte)(crchi ^ TABLE_8005_LO[index]);
                crchi = TABLE_8005_HI[index];
            }

            if (XHXL)
            {
                return (ushort)((crchi << 8 | crclo) ^ 0xFFFF);
            }
            else
            {
                return (ushort)((crclo << 8 | crchi) ^ 0xFFFF);
            }
        }

        //初值:0xFFFF
        //多项式:0x8005
        //结果异或:0xFFFF
        //test ok
        public ushort AP_CRC16_USB(Byte[] pData, int len, Boolean XHXL)
        {
            ushort CRC_Init_0x0000 = USB_INIT;

            Byte crchi;
            Byte crclo;
            Byte index;

            CRC_Init_0x0000 ^= 0xFFFF;

            crchi = (Byte)(CRC_Init_0x0000 >> 8);
            crclo = (Byte)(CRC_Init_0x0000);

            for (int i = 0; i < len; i++)
            {
                index = (Byte)(crclo ^ pData[i]);
                crclo = (Byte)(crchi ^ TABLE_8005_LO[index]);
                crchi = TABLE_8005_HI[index];
            }

            if (XHXL)
            {
                return (ushort)((crchi << 8 | crclo) ^ 0xFFFF);
            }
            else
            {
                return (ushort)((crclo << 8 | crchi) ^ 0xFFFF);
            }
        }

        //异或校验test ok
        public ushort AP_BCC(Byte[] pData, int len, Boolean XHXL)
        {
            Byte value = 0;
            Byte crchi = 0;
            Byte crclo = 0;

            for (int i = 0; i < len; i++)
            {
                value ^= pData[i];
            }

            crchi = (Byte)(value >> 4);
            crclo = (Byte)(value & 0x0F);

            if (crchi > 9)
            {
                crchi += 55;//A~F
            }
            else
            {
                crchi += 48;//0~9
            }

            if (crclo > 9)
            {
                crclo += 55;//A~F
            }
            else
            {
                crclo += 48;//0~9
            }

            if (XHXL)
            {
                return (ushort)(crchi << 8 | crclo);
            }
            else
            {
                return (ushort)(crclo << 8 | crchi);
            }
        }

        //test ok
        public ushort AP_LRC(Byte[] pData, int len, Boolean XHXL)
        {
            Byte value = 0;
            Byte crchi = 0;
            Byte crclo = 0;

            for (int i = 0; i < len; i++)
            {
                value += pData[i];
            }

            value = (Byte)((~value) + 1);

            crchi = (Byte)(value >> 4);
            crclo = (Byte)(value & 0x0F);

            if (crchi > 9)
            {
                crchi += 55;
            }
            else
            {
                crchi += 48;
            }

            if (crclo > 9)
            {
                crclo += 55;
            }
            else
            {
                crclo += 48;
            }

            if (XHXL)
            {
                return (ushort)(crchi << 8 | crclo);
            }
            else
            {
                return (ushort)(crclo << 8 | crchi);
            }
        }
    }
}

//end
