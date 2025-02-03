using System;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace SuperMiniRendering
{
    #region 一、状态设置 ：  枚举（渲染模式选择、渲染颜色选择）


    //选择渲染模式：
    public enum RenderMode
    {
        Wireframe,
        BackfaceCulling,
        Shading
    }

    //渲染颜色
    public enum CellType
    {
        White,//0
        Gray,//1
        Black,//2
        DarkGray//3

    }


    #endregion
    #region 二、前期定义 & 模型输入： （向量结构体+输入模型数据+封装运算方法）


    //定义一个结构体 表示三维向量
    public struct Vector3 //输入“三个浮点值”，输出“坐标字符串”
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        //打印向量坐标需要重写ToString方法：
        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }

    //定义一个正方体
    public struct Cube
    {
        //定义一个点的数组，以Vector3为元素的一维数组
        public Vector3[] cubeV3;
        public int Size;
        public int[] edges;//存储边索引的一维数组
        public int[] triangles;//三角面索引
        public Vector3[] faceNormals;//三角面的法向量
        //构造函数
        public Cube(int size)
        {
            Size = size;
            //初始化顶点数组
            cubeV3 = new Vector3[8];
            cubeV3[0] = new Vector3(0, 0, 0);          // 前下左
            cubeV3[1] = new Vector3(size, 0, 0);       // 前下右
            cubeV3[2] = new Vector3(size, 0, size);    // 后下右
            cubeV3[3] = new Vector3(0, 0, size);       // 后下左
            cubeV3[4] = new Vector3(0, size, 0);       // 前上左
            cubeV3[5] = new Vector3(size, size, 0);    // 前上右
            cubeV3[6] = new Vector3(size, size, size); // 后上右
            cubeV3[7] = new Vector3(0, size, size);    // 后上左

            //初始化边索引
            edges = new int[]
            {
                 // 底面的4条边
            0, 1,   // 前下
            1, 2,   // 右下
            2, 3,   // 后下
            3, 0,   // 左下
            // 顶面的4条边
            4, 5,   // 前上
            5, 6,   // 右上
            6, 7,   // 后上
            7, 4,   // 左上
            // 连接顶面和底面的4条边
            0, 4,   // 左前
            1, 5,   // 右前
            2, 6,   // 右后
            3, 7    // 左后
            };

            // 12个三角形(6个面，每个面2个三角形)
            triangles = new int[]
            {
               // 前面 (顺时针)
                0, 4, 1,  1, 4, 5,
                // 右面
                1, 5, 2,  2, 5, 6,
                // 后面
                2, 6, 3,  3, 6, 7,
                // 左面
                3, 7, 0,  0, 7, 4,
                // 上面
                4, 7, 5,  5, 7, 6,
                // 下面
                0, 1, 3,  1, 2, 3
            };

            // 初始化法向量数组(每个面一个法向量)
            faceNormals = new Vector3[6];
            CalculateFaceNormals();

        }

        private void CalculateFaceNormals()
        {
            for (int i = 0; i < 6; i++) // 6个面
            {
                int triIndex = i * 6; // 每个面有6个顶点索引(2个三角形)
                Vector3 v0 = cubeV3[triangles[triIndex]];
                Vector3 v1 = cubeV3[triangles[triIndex + 1]];
                Vector3 v2 = cubeV3[triangles[triIndex + 2]];

                // 计算法向量 (v1-v0)×(v2-v0)
                Vector3 edge1 = new Vector3(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z);
                Vector3 edge2 = new Vector3(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z);
                faceNormals[i] = CrossProduct(edge1, edge2);
                faceNormals[i] = Normalize(faceNormals[i]);
            }
        }

        // 辅助方法
        //叉乘
        public static Vector3 CrossProduct(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }
        //归一化
        public static Vector3 Normalize(Vector3 v)
        {
            float length = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            return new Vector3(v.X / length, v.Y / length, v.Z / length);
        }
        //点乘
        public static float DotProduct(Vector3 a, Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public void Rotate(float angleX, float angleY)
        {
            // 创建旋转矩阵
            float radX = angleX * (float)Math.PI / 180.0f;
            float radY = angleY * (float)Math.PI / 180.0f;

            // 绕Y轴旋转
            float cosY = (float)Math.Cos(radY);
            float sinY = (float)Math.Sin(radY);

            // 绕X轴旋转
            float cosX = (float)Math.Cos(radX);
            float sinX = (float)Math.Sin(radX);

            // 旋转每个顶点
            for (int i = 0; i < cubeV3.Length; i++)
            {
                float x = cubeV3[i].X - Size / 2;
                float y = cubeV3[i].Y - Size / 2;
                float z = cubeV3[i].Z - Size / 2;

                // 绕Y轴旋转
                float newX = x * cosY - z * sinY;
                float newZ = x * sinY + z * cosY;

                // 绕X轴旋转
                float newY = y * cosX - newZ * sinX;
                newZ = y * sinX + newZ * cosX;

                cubeV3[i] = new Vector3(
                    newX + Size / 2,
                    newY + Size / 2,
                    newZ + Size / 2
                );
            }

            // 重新计算法向量
            CalculateFaceNormals();
        }


    }

    public struct Edge
    {
        public Vector3 start;
        public Vector3 end;
        public bool isShared;

        public Edge(Vector3 s, Vector3 e)
        {
            start = s;
            end = e;
            isShared = false;
        }

        // 添加相等性比较方法，用于字典键
        public override bool Equals(object obj)
        {
            if (!(obj is Edge)) return false;
            Edge other = (Edge)obj;
            return (start.Equals(other.start) && end.Equals(other.end)) ||
                   (start.Equals(other.end) && end.Equals(other.start));
        }

        public override int GetHashCode()
        {
            return start.GetHashCode() ^ end.GetHashCode();
        }
    }

    #endregion
    #region 三、渲染：  把模型输入的3D坐标通过（正交、透视、旋转）等方式转换成2D坐标


    public class Render
    {

        //世界坐标系 --视图矩阵--> 相机坐标系 --投影矩阵--> 裁剪空间(视锥体)
        //如果视图矩阵将顶点坐标从世界坐标系变换为相机坐标系 - 平移与旋转

        //视图矩阵 be like:
        //[R11 R12 R13 T1]
        //[R21 R22 R23 T2]
        //[R31 R32 R33 T3]
        //[0   0   0   1 ]

        //R代表旋转 也代表相机坐标系的三个基向量（Right，Up，Look）这里显示为三个行矩阵
        //T代表平移，即相机Eye的坐标点

        //输入值：顶点坐标、相机位置坐标、相机的三个基向量
        //过程：将【顶点坐标的列向量】和【由（相机位置坐标）和（相机的三个基向量）组成的视图矩阵】进行矩阵乘法（与点乘不同）
        //输出值：新的顶点坐标
        #region 光照参数
        // 光照角度
        private float LcurrentAngleX = 0f;
        private float LcurrentAngleY = 0f;
        private float LcurrentAngleZ = 0f;
 
        private Vector3 lightDirection;  
                                         
                                         // 新增：光线的起点和终点
        private Vector3 lightStart;      // 这里添加
        private Vector3 lightEnd;        // 这里添加
        private float lightLength = 10f;  // 这里添加
        public Render()
        {
            lightDirection = Cube.Normalize(new Vector3(0,-0.7f, -0.6f));
            // ... 其他初始化代码 ...
            UpdateLightPoints();
        }
        // 添加一个更新光线端点的方法
        private void UpdateLightPoints()
        {
            // 选择一个中心点作为参考
            Vector3 center = new Vector3(15, -15, 0);  // 可以根据需要调

            // 起点 = 中心点 - (光线方向 * 长度/2)
            lightStart = new Vector3(
                center.X - lightDirection.X * lightLength / 2,
                center.Y - lightDirection.Y * lightLength / 2,
                center.Z - lightDirection.Z * lightLength / 2
            );

            // 终点 = 中心点 + (光线方向 * 长度/2)
            lightEnd = new Vector3(
                center.X + lightDirection.X * lightLength / 2,
                center.Y + lightDirection.Y * lightLength / 2,
                center.Z + lightDirection.Z * lightLength / 2
            );
        }
        // 添加获取端点的方法
        public Vector3 GetLightStart()
        {
            return lightStart;
        }

        public Vector3 GetLightEnd()
        {
            return lightEnd;
        }
        #endregion
        #region 零、设定相机参数




        //0.2 相机位置坐标：Vector3View - 可参数化调整
        public Vector3 v3CameraPlace = new Vector3(0, 0, 100);
        //相机的正交or透视：
        public bool isProject = false;

        //0.3 相机的基向量：Vector3Right, Vector3Up, Vector3Look,
        public Vector3 v3CameraRight = new Vector3(1, 0, 0);
        public Vector3 v3CameraUp = new Vector3(0, 1, 0);
        public Vector3 v3CameraLook = new Vector3(0, 0, 1);

        // 添加成员变量存储当前角度
        private float currentAngleX = 0f;
        private float currentAngleY = 0f;
        private float currentAngleZ = 0f;
        //旋转相机方法 - 初始化
        public void RotateCamera(float angleX, float angleY, float angleZ)
        {
            // 设置初始角度
            currentAngleX = angleX;
            currentAngleY = angleY;
            currentAngleZ = angleZ;

            // 计算弧度
            float radX = currentAngleX * (float)Math.PI / 180.0f;
            float radY = currentAngleY * (float)Math.PI / 180.0f;
            float radZ = currentAngleZ * (float)Math.PI / 180.0f;

            // 绕Y轴旋转
            v3CameraRight.X = (float)Math.Cos(radY);
            v3CameraRight.Z = -(float)Math.Sin(radY);

            // 绕X轴旋转
            v3CameraUp.Y = (float)Math.Cos(radX);
            v3CameraUp.Z = -(float)Math.Sin(radX);

            // Look向量需要相应更新
            v3CameraLook.X = (float)Math.Sin(radY);
            v3CameraLook.Y = (float)Math.Sin(radX);
            v3CameraLook.Z = (float)Math.Cos(radX) * (float)Math.Cos(radY);
        }
        //优化：初始化带相机旋转参数
        //优化：旋转相机方法 - 修改
        public void RotateCameraChange(float deltaAngleX, float deltaAngleY, float deltaAngleZ)
        {
            // 累加角度
            currentAngleX += deltaAngleX;
            currentAngleY += deltaAngleY;
            currentAngleZ += deltaAngleZ;

            // 计算弧度
            float radX = currentAngleX * (float)Math.PI / 180.0f;
            float radY = currentAngleY * (float)Math.PI / 180.0f;
            float radZ = currentAngleZ * (float)Math.PI / 180.0f;

            // 绕Y轴旋转
            v3CameraRight.X = (float)Math.Cos(radY);
            v3CameraRight.Z = -(float)Math.Sin(radY);

            // 绕X轴旋转
            v3CameraUp.Y = (float)Math.Cos(radX);
            v3CameraUp.Z = -(float)Math.Sin(radX);

            // Look向量需要相应更新
            v3CameraLook.X = (float)Math.Sin(radY);
            v3CameraLook.Y = (float)Math.Sin(radX);
            v3CameraLook.Z = (float)Math.Cos(radX) * (float)Math.Cos(radY);
        }
        #endregion
        #region 一、世界坐标系 -> 相机坐标系 - 世界-相机矩阵


        //矩阵变换 世界-相机坐标系的矩阵方法
        float[,] matrixView = new float[4, 4];
        public void MatrixView()
        {
            matrixView[0, 0] = v3CameraRight.X;
            matrixView[0, 1] = v3CameraRight.Y;
            matrixView[0, 2] = v3CameraRight.Z;
            matrixView[0, 3] = -v3CameraPlace.X;  // 平移：相机位置

            matrixView[1, 0] = v3CameraUp.X;
            matrixView[1, 1] = v3CameraUp.Y;
            matrixView[1, 2] = v3CameraUp.Z;
            matrixView[1, 3] = -v3CameraPlace.Y;  // 平移

            matrixView[2, 0] = v3CameraLook.X;
            matrixView[2, 1] = v3CameraLook.Y;
            matrixView[2, 2] = v3CameraLook.Z;
            matrixView[2, 3] = -v3CameraPlace.Z;  // 平移

            matrixView[3, 0] = 0;
            matrixView[3, 1] = 0;
            matrixView[3, 2] = 0;
            matrixView[3, 3] = 1;
        }

        public Vector3 ViewTrans(Vector3 v) //方法：输入“旧三维坐标 - 世界坐标系”，输出“新三维坐标 - 相机坐标系”
        {
            Canvas canvas = new Canvas();
            //计算相机坐标系下的坐标
            float x = v.X * matrixView[0, 0] + v.Y * matrixView[0, 1] + v.Z * matrixView[0, 2] + matrixView[0, 3]; //调用矩阵变换的方法
            float y = v.X * matrixView[1, 0] + v.Y * matrixView[1, 1] + v.Z * matrixView[1, 2] + matrixView[1, 3]; 
            float z = v.X * matrixView[2, 0] + v.Y * matrixView[2, 1] + v.Z * matrixView[2, 2] + matrixView[2, 3];

            //返回变换后的向量
            //Console.WriteLine(new Vector3(x, y, z));
            return new Vector3(x, y, z);
        }
        #endregion
        //投影矩阵 - 由相机坐标系到裁剪空间（视椎体）
        //视椎体到裁剪空间的转换保持了基向量的正交性，
        //只是改变了坐标系的范围
        //（把不规则的视椎体变成标准立方体）。
        //变形效果是由于透视除法(除以w)导致的，而不是基向量的改变。

        //连线方法：输入（2个二维坐标），输出（之间每个点的坐标的数组）


        #region 二、相机坐标系到裁剪空间 - 透视投影矩阵
        //透视矩阵方法：
        //输入：齐次后的相机坐标系坐标。输出：对应的裁剪空间的点

        public float[,] MatrixProjection(float fov, float aspect, float near, float far)
        {
            float[,] matrix = new float[4, 4];
            // 初始化为0
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    matrix[i, j] = 0;

            float tanHalfFov = (float)Math.Tan(fov * 0.5f * Math.PI / 180.0f);
            float zRange = near - far;

            matrix[0, 0] = 1.0f / (tanHalfFov * aspect);
            matrix[1, 1] = 1.0f / tanHalfFov;
            matrix[2, 2] = (-near - far) / zRange;
            matrix[2, 3] = 2 * far * near / zRange;
            matrix[3, 2] = 1.0f;

            return matrix;
        }
        #endregion
        #region 三、旋转模型


        public void RotateModel(float angleX, float angleY)
        {
            float radX = angleX * (float)Math.PI / 180.0f;
            float radY = angleY * (float)Math.PI / 180.0f;

            // 创建旋转矩阵
            float[,] rotationMatrix = new float[4, 4];
            rotationMatrix[0, 0] = (float)Math.Cos(radY);
            rotationMatrix[0, 2] = (float)Math.Sin(radY);
            rotationMatrix[1, 1] = (float)Math.Cos(radX);
            rotationMatrix[1, 2] = (float)-Math.Sin(radX);
            rotationMatrix[2, 0] = (float)-Math.Sin(radY);
            rotationMatrix[2, 1] = (float)Math.Sin(radX);
            rotationMatrix[2, 2] = (float)Math.Cos(radX) * (float)Math.Cos(radY);
            rotationMatrix[3, 3] = 1;

            // 更新相机向量
            v3CameraRight = new Vector3(rotationMatrix[0, 0], rotationMatrix[0, 1], rotationMatrix[0, 2]);
            v3CameraUp = new Vector3(rotationMatrix[1, 0], rotationMatrix[1, 1], rotationMatrix[1, 2]);
            v3CameraLook = new Vector3(rotationMatrix[2, 0], rotationMatrix[2, 1], rotationMatrix[2, 2]);
        }
        #endregion
        #region 光照
        // 添加这两个方法
        public void RotateLight(float deltaAngleX, float deltaAngleY, float deltaAngleZ)
        {
            // 1. 将角度变化转换为弧度
            float angleX = deltaAngleX * (float)Math.PI / 180.0f;
            float angleY = deltaAngleY * (float)Math.PI / 180.0f;

            // 2. 获取当前的光照方向
            Vector3 direction = lightDirection;

            // 3. Y轴旋转（左右）
            if (deltaAngleY != 0)
            {
                float cosY = (float)Math.Cos(angleY);
                float sinY = (float)Math.Sin(angleY);
                float x = direction.X;
                float z = direction.Z;
                direction.X = x * cosY + z * sinY;
                direction.Z = -x * sinY + z * cosY;
            }

            // 4. X轴旋转（上下）
            if (deltaAngleX != 0)
            {
                float cosX = (float)Math.Cos(angleX);
                float sinX = (float)Math.Sin(angleX);
                float y = direction.Y;
                float z = direction.Z;
                direction.Y = y * cosX + z * sinX;
                direction.Z = -y * sinX + z * cosX;
            }

            // 5. 归一化并更新方向
            lightDirection = Cube.Normalize(direction);
            UpdateLightPoints();
        }
        public Vector3 GetLightDirection()
        {
            return lightDirection;
        }
        #endregion

    }
    #endregion
    #region 四、绘制画布 ：   把得到的2D坐标通过（画线、背面剔除）等方式打印出来


    //如何把这个正方体类的每个经过转换的二维顶点打印出来
    //1.绘制方法：提交一个顶点数组 就能绘制出来的方法

    //定义一个类 表示画布
    public class Canvas
    {
        private float[,] frontBuffer; // 前缓冲区(显示)
        private float[,] backBuffer;  // 后缓冲区(绘制)
        private int height = 40;      // 原来的高度
        private int width = 80;       // 原来的宽度

        public Canvas()
        {
            frontBuffer = new float[height, width];
            backBuffer = new float[height, width];
            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            // 初始化两个缓冲区为白色
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    frontBuffer[i, j] = (float)CellType.White;
                    backBuffer[i, j] = (float)CellType.White;
                }
        }

        //渲染模式：
        private RenderMode currentMode = RenderMode.Wireframe;

        public void SetRenderMode(RenderMode mode)
        {
            currentMode = mode;
        }

        //定义一个二维数组 再打印变成画布 - 二维数组arr2d[y,x]是先行后列
        //float[,] arr2d = new float[40, 80];
        //private float[,] frameBuffer = new float[50, 80];  // 新增帧缓冲区



        //绘制内容
        public void DrawContent(Cube cube, Render render) //绘制内容方法：输入“正方体结构体+渲染类” 输出“打印二维坐标”
        {
            #region 初始化画布


            // 清空后缓冲区
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    backBuffer[i, j] = (float)CellType.White;

            #endregion

            #region 正交投影 （输入：相机坐标系的3D坐标 过程：只获取xy 输出：2D正交坐标）
            //初始化视图矩阵
            render.MatrixView();
            // 初始化向量数组 - 正方体顶点的投影后的坐标
            Vector3[] projectedPoints = new Vector3[cube.cubeV3.Length];
            Vector3[] projectedPointsP = new Vector3[cube.cubeV3.Length];

            // 1 计算正交投影点
            for (int i = 0; i < cube.cubeV3.Length; i++) //遍历cube内的一维数组
            {
                //获取正方体顶点的相机坐标：
                Vector3 v = render.ViewTrans(cube.cubeV3[i]); //v是顶点的相机坐标 - 去掉z轴直接打印
                
                //正交投影：
                //获取x坐标
                int canvasX = (int)(v.X+ width / 2 - cube.Size / 2);
                //获取y坐标
                int canvasY = (int)(v.Y + height/ 2 - cube.Size / 2);
                // GetLength(1)代表二维数组的列数（宽度），而GetLength(0)代表行数（高度）。
                if (IsInCanvas(canvasX, canvasY))
                {
                    //arr2d[canvasY, canvasX] = (float)CellType.Black;//绘制顶点
                    projectedPoints[i] = new Vector3(canvasX, canvasY, v.Z);
                    //Console.WriteLine("正方体顶点在画布内，打印顶点");
                }   //成功让arr2d这个二维数组变成了有内容的样子 现在需要打印这个二维数组
            }
            #endregion

            #region 透视投影 （输入：相机坐标系的3D坐标 过程：点乘透视矩阵 输出：2D透视坐标）
            // 2. 计算透视投影点
            // 初始化向量数组 - 正方体顶点的投影后的坐标
            Vector3[] projectedPointP = new Vector3[cube.cubeV3.Length];
            //初始化相机参数
            float fov = 25.0f;//视场角
            float aspect = (float)width / height;//宽高比
            float near = 0.1f;//近裁剪平面
            float far = 100.0f;//远裁剪平面
            //调用透视投影方法 生成透视投影矩阵 并存储在新二维数组中
            float[,] projectionMatrix = render.MatrixProjection(fov, aspect, near, far);
            //应用透视投影：
            for (int i = 0; i < cube.cubeV3.Length; i++)//遍历正方体顶点
            {
                Vector3 v = render.ViewTrans(cube.cubeV3[i]);//相机坐标系中的点
                Vector3 projectedPoint = ProjectPoint(v, projectionMatrix);//透视投影后的点
                //                                                           //映射到屏幕空间
                //                                                           //1.将NDC的xy从[-1.1]映射到[0,1] ：xy / 2 + 0.5
                //                                                           //2.将[0,1]映射到屏幕空间 ：xy乘以屏幕尺寸
                //int canvasX = (int)((projectedPoint.X + 1) * 0.5f * arr2d.GetLength(1) - cube.Size / 2);
                //int canvasY = (int)((1 - projectedPoint.Y) * 0.5f * arr2d.GetLength(0) - cube.Size / 2);

                //if (IsInCanvas(canvasX, canvasY))
                //{
                //    projectedPointsP[i] = new Vector3(canvasX, canvasY, projectedPoint.Z);
                //}
                float centerX = width / 2.0f;
                float centerY = height / 2.0f;

                // 坐标映射时考虑屏幕中心点偏移
                int canvasX = (int)(centerX + projectedPoint.X * centerX);  // 将[-1,1]映射到[0,width]
                int canvasY = (int)(centerY - projectedPoint.Y * centerY);  // Y轴需要翻转,因为屏幕坐标Y轴向下

                // 确保点在有效范围内
                if (canvasX < 0) canvasX = 0;
                if (canvasX >= width) canvasX = width - 1;
                if (canvasY < 0) canvasY = 0;
                if (canvasY >= height) canvasY = height - 1;

                if (IsInCanvas(canvasX, canvasY))
                {
                    //Console.WriteLine($"NDC coordinates: ({projectedPoint.X}, {projectedPoint.Y})");
                    //Console.WriteLine($"Screen coordinates: ({canvasX}, {canvasY})");
                    projectedPointsP[i] = new Vector3(canvasX, canvasY, projectedPoint.Z);
                }
            }


            #endregion
            #region 枚举对应渲染模式 - 分别调用不同的渲染方法

            if (!render.isProject)
            {
                switch (currentMode) //不同的渲染模式（枚举）调用不同的方法：把枚举和方法链接
                {
                    case RenderMode.Wireframe:
                        DrawWireframe(cube, projectedPoints);
                        break;
                    case RenderMode.BackfaceCulling:
                        DrawBackfaceCulling(cube, projectedPoints, render);
                        break;
                    case RenderMode.Shading:
                        DrawShading(cube, projectedPoints, render);
                        break;
                }
            } 
            else
            {   
                switch (currentMode) //不同的渲染模式（枚举）调用不同的方法：把枚举和方法链接
                {
                    case RenderMode.Wireframe:
                        DrawWireframe(cube, projectedPointsP);
                        break;
                    case RenderMode.BackfaceCulling:
                        DrawBackfaceCulling(cube, projectedPointsP, render);
                        break;
                    case RenderMode.Shading:
                        
                        DrawShading(cube, projectedPointsP, render);
                        break;
                }
                
            }

            #endregion

            // 渲染画布
            RenderCanvas();

        }
        #region 渲染方法1：线框

    
        private void DrawWireframe(Cube cube, Vector3[] projectedPoints)
        {

            for (int i = 0; i < cube.edges.Length; i += 2)
            {
                DrawLine(projectedPoints[cube.edges[i]], projectedPoints[cube.edges[i + 1]]);
            }
        }
        #endregion
        #region 渲染方法2：背面剔除


        //背面剔除（输入：正方体结构体，投影后的点的数组，渲染器初始化。输出：通过两点调用画线）
        private void DrawBackfaceCulling(Cube cube, Vector3[] projectedPoints, Render render)
        {
            
            for (int i = 0; i < cube.triangles.Length; i += 3)
            {
                int v0Index = cube.triangles[i];
                int v1Index = cube.triangles[i + 1];
                int v2Index = cube.triangles[i + 2];

                Vector3 screenEdge1 = new Vector3(
                    projectedPoints[v1Index].X - projectedPoints[v0Index].X,
                    projectedPoints[v1Index].Y - projectedPoints[v0Index].Y,
                    projectedPoints[v1Index].Z - projectedPoints[v0Index].Z  // 添加 Z 坐标
                );

                Vector3 screenEdge2 = new Vector3(
                    projectedPoints[v2Index].X - projectedPoints[v0Index].X,
                    projectedPoints[v2Index].Y - projectedPoints[v0Index].Y,
                    projectedPoints[v2Index].Z - projectedPoints[v0Index].Z  // 添加 Z 坐标
                );
                //float removeLine = Cube.DotProduct(screenEdge1, screenEdge2);

                Vector3 screenNormal = Cube.CrossProduct(screenEdge1, screenEdge2);

                // 使用视线向量
                Vector3 viewDir = new Vector3(0, 0, 1);  // 假设视线方向是正 Z 轴
                float dotProduct = Cube.DotProduct(screenNormal, viewDir);

                

                if (dotProduct < 0)  // 修改判断条件
                {
                    if (!IsSharedEdge(cube.cubeV3[v0Index], cube.cubeV3[v1Index], cube))
                    {
                        //Console.WriteLine("这两个点组成的线，不是斜线，可以绘制。IsSharedEdge为假，该线不共面，绘制。");
                        DrawLine(projectedPoints[v0Index], projectedPoints[v1Index]);
                    }
    
                    if (!IsSharedEdge(cube.cubeV3[v1Index], cube.cubeV3[v2Index], cube))
                    {
                        DrawLine(projectedPoints[v1Index], projectedPoints[v2Index]);
                    }
                    if (!IsSharedEdge(cube.cubeV3[v2Index], cube.cubeV3[v0Index], cube))
                    {
                        DrawLine(projectedPoints[v2Index], projectedPoints[v0Index]);
                    }          
                }
            }
        }
        #endregion
        #region 渲染方法3：剔除共面线

   
        //剔除共面线：
        // 在 Canvas 类中添加新方法：
        private bool IsSharedEdge(Vector3 point1, Vector3 point2, Cube cube)
        {
            //检查这对点在哪个面上
            for(int faceIndex = 0;faceIndex < 6; faceIndex++)
            {
                //每个面由6个顶点索引组成（两个三角形）
                //计算每个面的起始顶点索引
                int startIndex = faceIndex * 6;

                //获取这个面的6个顶点索引
                int[] faceVertices = new int[]
                {
                    cube.triangles[startIndex],
                    cube.triangles[startIndex + 1],
                    cube.triangles[startIndex + 2],
                    cube.triangles[startIndex + 3],
                    cube.triangles[startIndex + 4],
                    cube.triangles[startIndex + 5]
                };

                //计算point1和point2在这个面的6个顶点中出现几次
                int point1Count = 0;
                int point2Count = 0;
                foreach (int vertexIndex in faceVertices)
                {
                    if (cube.cubeV3[vertexIndex].Equals(point1)) point1Count++;
                    if (cube.cubeV3[vertexIndex].Equals(point2)) point2Count++;
                }
               // Console.WriteLine($"这两个点组成的线，起点在该面出现{point1Count}次，终点在该面出现{point2Count}次");
                // 如果两个点都出现超过1次，说明是共面线
                if (point1Count > 1 && point2Count > 1)
                {
                   // Console.WriteLine($"这两个点组成的线，不是斜线，可以绘制");

                    return true;
                }
            }
            
            return false;
        }
        #endregion
        #region 渲染方法4：画线


        private void DrawLine(Vector3 start, Vector3 end)
        {
            int x0 = (int)start.X;
            int y0 = (int)start.Y;
            int x1 = (int)end.X;
            int y1 = (int)end.Y;
            //Console.WriteLine($"Draw line from ({x0}, {y0}) to ({x1}, {y1})");

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (IsInCanvas(x0, y0))
                    backBuffer[y0, x0] = (float)CellType.Gray; // 修改这行


                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
        #endregion
        #region 渲染方法5：枚举对应打印的符号


        //枚举对应符号方法：(输入“枚举类型”，输出“符号字符串”)
        public string GetSymbol(CellType type)
        {
            switch (type)
            {
                case CellType.White:
                    return "  ";//∷ 
                case CellType.Gray:
                    return "■ ";//■ 
                case CellType.Black:
                    return "∷ ";
                case CellType.DarkGray:
                    return "〼 ";
                default:
                    return "  ";
            }
        }
        #endregion
        #region 光照

        private void DrawShading(Cube cube, Vector3[] projectedPoints, Render render)
        {
            Vector3 lightDir = render.GetLightDirection();
            // 添加：获取光源的两个端点
            Vector3 lightStart = render.GetLightStart();
            Vector3 lightEnd = render.GetLightEnd();

            // 添加：将光源的两个端点转换到投影空间
            Vector3 projectedStart = render.ViewTrans(lightStart);
            Vector3 projectedEnd = render.ViewTrans(lightEnd);

            // 添加：如果是透视模式，需要进行透视投影变换
            if (render.isProject)
            {
                float fov = 25.0f;
                float aspect = (float)width / height;
                float near = 1.5f;
                float far = 40.0f;
                float[,] projectionMatrix = render.MatrixProjection(fov, aspect, near, far);

                projectedStart = ProjectPoint(projectedStart, projectionMatrix);
                projectedEnd = ProjectPoint(projectedEnd, projectionMatrix);

                // 转换到屏幕坐标
                float centerX = width / 2.0f;
                float centerY = height / 2.0f;
                projectedStart = new Vector3(
                    centerX + projectedStart.X * centerX,
                    centerY + projectedStart.Y * centerY,  // 从减号改为加号
                    projectedStart.Z
                );
                projectedEnd = new Vector3(
                    centerX + projectedEnd.X * centerX,
                    centerY + projectedEnd.Y * centerY,    // 从减号改为加号
                    projectedEnd.Z
                );
            }
            else
            {
                // 正交模式下的坐标转换
                projectedStart = new Vector3(
                    projectedStart.X + width / 2,
                    projectedStart.Y + height / 2,
                    projectedStart.Z
                );
                projectedEnd = new Vector3(
                    projectedEnd.X + width / 2,
                    projectedEnd.Y + height / 2,
                    projectedEnd.Z
                );
            }
            // 添加：绘制光源方向线
            DrawLine(projectedStart, projectedEnd);
            DrawArrowhead(projectedStart, projectedEnd);  // 添加箭头

            for (int i = 0; i < cube.triangles.Length; i += 3)
            {
                int faceIndex = i / 6;
                Vector3 faceNormal = cube.faceNormals[faceIndex];

                // 关键修改：在透视模式下反转法向量判断
                float dotProductView;
                if (render.isProject)
                {
                    // 透视模式下反转判断条件
                    dotProductView = Cube.DotProduct(faceNormal, render.v3CameraLook);
                    if (dotProductView > 0)  // 注意这里改成了 > 0
                    {
                        float intensity = Math.Max(0, Cube.DotProduct(faceNormal, lightDir));

                        CellType shadingType;
                        if (intensity > 0.7f)
                            shadingType = CellType.Gray;
                        else if (intensity > 0.3f)
                            shadingType = CellType.Black;
                        else
                            shadingType = CellType.DarkGray;

                        DrawTriangle(
                            projectedPoints[cube.triangles[i]],
                            projectedPoints[cube.triangles[i + 1]],
                            projectedPoints[cube.triangles[i + 2]],
                            shadingType
                        );
                    }
                }
                else
                {
                    // 正交模式保持原判断
                    dotProductView = Cube.DotProduct(faceNormal, render.v3CameraLook);
                    // 只有当面朝向观察者时才进行光照计算和渲染
                    if (dotProductView < 0)  // 或 > 0，取决于透视/正交模式
                    {
                        // 计算光照强度(点乘)
                        float intensity = Math.Max(0, Cube.DotProduct(faceNormal, lightDir));

                        // 修改判断条件，让强度和枚举对应：
                        // Gray(1) 是最亮
                        // Black(2) 是中等
                        // DarkGray(3) 是最暗
                        CellType shadingType;
                        if (intensity > 0.7f)
                            shadingType = CellType.Gray;      // 最亮面用 1
                        else if (intensity > 0.3f)
                            shadingType = CellType.Black;     // 中等亮度用 2
                        else
                            shadingType = CellType.DarkGray;  // 最暗面用 3

                        DrawTriangle(
                            projectedPoints[cube.triangles[i]],
                            projectedPoints[cube.triangles[i + 1]],
                            projectedPoints[cube.triangles[i + 2]],
                            shadingType
                        );
                    }
                }
            }


        }


        // 添加绘制填充三角形的方法
        private void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, CellType cellType)
        {
            // 简单包围盒方法
            int minX = (int)Math.Min(v0.X, Math.Min(v1.X, v2.X));
            int maxX = (int)Math.Max(v0.X, Math.Max(v1.X, v2.X));
            int minY = (int)Math.Min(v0.Y, Math.Min(v1.Y, v2.Y));
            int maxY = (int)Math.Max(v0.Y, Math.Max(v1.Y, v2.Y));

            // 遍历包围盒中的每个像素
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (IsInCanvas(x, y) && IsPointInTriangle(x, y, v0, v1, v2))
                    {
                        backBuffer[y, x] = (float)cellType;
                    }
                }
            }
        }
        //添加箭头：
        private void DrawArrowhead(Vector3 end, Vector3 start)
        {
            float arrowSize = 3.0f;
            float angle = (float)Math.PI / 6;  // 确保角度是 float

            // 计算主方向向量
            Vector3 direction = new Vector3(
                end.X - start.X,
                end.Y - start.Y,
                end.Z - start.Z
            );

            // 计算向量长度用于归一化
            float vectorLength = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            // 左边箭头点
            Vector3 arrowLeft = new Vector3(
                end.X - arrowSize * ((float)(Math.Cos(angle) * direction.X / vectorLength) -
                                    (float)(Math.Sin(angle) * direction.Y / vectorLength)),
                end.Y - arrowSize * ((float)(Math.Sin(angle) * direction.X / vectorLength) +
                                    (float)(Math.Cos(angle) * direction.Y / vectorLength)),
                end.Z
            );

            // 右边箭头点
            Vector3 arrowRight = new Vector3(
                end.X - arrowSize * ((float)(Math.Cos(-angle) * direction.X / vectorLength) -
                                    (float)(Math.Sin(-angle) * direction.Y / vectorLength)),
                end.Y - arrowSize * ((float)(Math.Sin(-angle) * direction.X / vectorLength) +
                                    (float)(Math.Cos(-angle) * direction.Y / vectorLength)),
                end.Z
            );

            // 绘制箭头的两条线
            DrawLine(end, arrowLeft);
            DrawLine(end, arrowRight);
        }
        // 判断点是否在三角形内的辅助方法
        private bool IsPointInTriangle(float px, float py, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            float area = 0.5f * (-v1.Y * v2.X + v0.Y * (-v1.X + v2.X) + v0.X * (v1.Y - v2.Y) + v1.X * v2.Y);
            float s = 1 / (2 * area) * (v0.Y * v2.X - v0.X * v2.Y + (v2.Y - v0.Y) * px + (v0.X - v2.X) * py);
            float t = 1 / (2 * area) * (v0.X * v1.Y - v0.Y * v1.X + (v0.Y - v1.Y) * px + (v1.X - v0.X) * py);

            return s >= 0 && t >= 0 && (1 - s - t) >= 0;
        }
        #endregion
        #region 渲染方法6：判断是否在界内
        private bool IsInCanvas(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        #endregion
        #region 渲染方法7：将顶点和投影矩阵相乘
        //输入相机坐标系模型顶点（三维向量），投影矩阵4*4（二维数组）
        public Vector3 ProjectPoint(Vector3 point, float[,] projectionMatrix)
        {
            float x = point.X * projectionMatrix[0, 0] + point.Y * projectionMatrix[0, 1] + point.Z * projectionMatrix[0, 2] + projectionMatrix[0, 3];
            float y = point.X * projectionMatrix[1, 0] + point.Y * projectionMatrix[1, 1] + point.Z * projectionMatrix[1, 2] + projectionMatrix[1, 3];
            float z = point.X * projectionMatrix[2, 0] + point.Y * projectionMatrix[2, 1] + point.Z * projectionMatrix[2, 2] + projectionMatrix[2, 3];
            float w = point.X * projectionMatrix[3, 0] + point.Y * projectionMatrix[3, 1] + point.Z * projectionMatrix[3, 2] + projectionMatrix[3, 3];


            // 确保w不为0，并且符号正确
            if (Math.Abs(w) < float.Epsilon)
                w = float.Epsilon;
            else if (w < 0)
                w = -w;

            // 透视除法
            x /= w;
            y /= w;
            z /= w;
            // 在ProjectPoint方法中添加调试输出
            //Console.WriteLine($"Before projection: ({point.X}, {point.Y}, {point.Z})");
            //Console.WriteLine($"After projection: ({x}, {y}, {z})");
            //Console.WriteLine($"W value: {w}");
            return new Vector3(x, y, z);
        }
        #endregion

        #region 渲染方法总：打印二维数组 - 绘制画布


        private void RenderCanvas()
        {
            Program program = new Program();
            // 保存原来的光标位置
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            // 打印标题信息
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("世界上最小的渲染器！");
            Console.WriteLine();
            Console.WriteLine("按1：线框模式，按2：背面剔除模式，按3：光照模式");
            Console.WriteLine();
            Console.WriteLine("↑ ↓ ← → 控制物体旋转，W A S D 控制镜头旋转");
            Console.WriteLine();
            Console.WriteLine("I J K L 控制光照旋转");
            // 打印光源角度
            Console.SetCursorPosition(0, 10); // 设置光标位置，避免覆盖其他内容
            Console.WriteLine($"当前光源角度：X = {program.lightAngleX:F2}°, Y = {program.lightAngleY:F2}°");
            Console.WriteLine($"光源方向向量：({program.lightDir.X:F2}, {program.lightDir.Y:F2}, {program.lightDir.Z:F2})");

            Console.WriteLine("");
            Console.WriteLine("按X 切换 透视 / 正交");
            Console.WriteLine();

            // 设置绘制起始位置
            int startRow = 8; // 标题后的起始行

            // 只更新变化的部分
            for (int i = 0; i < height; i++)
            {
                Console.SetCursorPosition(0, startRow + i);
                for (int j = 0; j < width; j++)
                {
                    if (backBuffer[i, j] != frontBuffer[i, j])
                    {
                        CellType cellType = (CellType)backBuffer[i, j];
                        Console.Write(GetSymbol(cellType));
                        frontBuffer[i, j] = backBuffer[i, j];
                    }
                    else
                    {
                        Console.Write(GetSymbol((CellType)frontBuffer[i, j]));
                    }
                }
            }

            // 恢复光标位置
            Console.SetCursorPosition(originalLeft, originalTop);
        }


        #endregion

    }
    //定义一个类 表示渲染器
    #endregion
    #region 五、调用 & 实时监听： 通过键盘监听调用绘制画布和渲染效果


    public class Program
    {
        public float lightAngleX;
        public float lightAngleY;
        public Vector3 lightDir;
        
        bool openPrint = false;
        // 在类的开头定义标题文本
        //private static readonly string[] titleText = new string[]
        //{
        //"世界上最小的渲染器！",
        //"按1：线框模式，按2：背面剔除模式，按3：光照模式",
        //"↑ ↓ ← → 控制物体旋转，W A S D 控制镜头旋转",
        //"按X 切换 透视 / 正交"
        //};

        static void Main(string[] args)
        {
            Program program = new Program();
            // 初始化时打印一次
            if (!program.openPrint)
            {
                Console.WriteLine("世界上最小的渲染器！");
                Console.WriteLine("");

                Console.WriteLine("按1：线框模式，按2：背面剔除模式，按3：光照模式");
                Console.WriteLine("");

                Console.WriteLine("↑ ↓ ← → 控制物体旋转，W A S D 控制镜头旋转");
                Console.WriteLine("");
                Console.WriteLine($"I J K L 控制光照旋转");
        
                Console.WriteLine("");
                Console.WriteLine("按X 切换 透视 / 正交");
                Console.WriteLine("");
                Console.WriteLine("I J K L 控制光照旋转");
                Console.WriteLine("");

                program.openPrint = true;
            }




            Canvas canvas = new Canvas();

            Render render = new Render();
            render.RotateCamera(30, 20, 20);

            Cube cube1 = new Cube(15);
            //canvas.DrawContent(cube1, render);


            //while (true)
            //{
            //    ConsoleKeyInfo key = Console.ReadKey(true); // 捕获按键

            //    if (key.Key == ConsoleKey.A) // 按下 A 键
            //    {
            //        Console.Clear(); // 清屏
            //        Console.WriteLine("刷新并打印"); // 输出新内容
            //    }
            //    else if (key.Key == ConsoleKey.Escape) // 按下 ESC 键
            //    {
            //        break; // 退出循环
            //    }
            //}
            // 选择模式和交互
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);

                    switch (key.Key)
                    {
                        case ConsoleKey.RightArrow:  // 右键
                            cube1.Rotate(0, 10);
                            break;
                        case ConsoleKey.LeftArrow:   // 左键
                            cube1.Rotate(0, -10);
                            break;
                        case ConsoleKey.DownArrow:     // 下键
                            cube1.Rotate(10, 0);
                            break;
                        case ConsoleKey.UpArrow:   // 上键
                            cube1.Rotate(-10, 0);
                            break;

                        case ConsoleKey.I:
                            render.RotateLight(-10, 0, 0);
                            break;
                        case ConsoleKey.K:
                            render.RotateLight(10, 0, 0);
                            break;
                        case ConsoleKey.J:
                            render.RotateLight(0, -10, 0);
                            break;
                        case ConsoleKey.L:
                            render.RotateLight(0, 10, 0);
                            break;

                        case ConsoleKey.A:  // 右键
                            render.RotateCameraChange(0, 10, 0);
                            break;
                        case ConsoleKey.D:   // 左键
                            render.RotateCameraChange(0, -10, 0);
                            break;
                        case ConsoleKey.S:     // 下键
                            render.RotateCameraChange(10, 0, 0);
                            break;
                        case ConsoleKey.W:   // 上键
                            render.RotateCameraChange(-10, 0, 0);
                            break;

                        case ConsoleKey.X:   // 切换正交
                            render.isProject = !render.isProject;
                            
                            break;

                        case ConsoleKey.D1:
                           
       
                            canvas.SetRenderMode(RenderMode.Wireframe);
                            break;
                        case ConsoleKey.D2:
                           
                        
                            canvas.SetRenderMode(RenderMode.BackfaceCulling);
                            break;
                        case ConsoleKey.D3:
                    
                            canvas.SetRenderMode(RenderMode.Shading);
                            break;
                    }
                    //Console.Clear();
                    


                    canvas.DrawContent(cube1, render);

                    // 打印光源角度
                    PrintLightDirection(render);
                }
            }
        }

        public static void PrintLightDirection(Render render)
        {
            Program p = new Program();
            // 获取当前光源方向
            p.lightDir = render.GetLightDirection();

            // 计算光源的角度（以度为单位）
            p.lightAngleX = (float)(Math.Atan2(p.lightDir.Y, p.lightDir.Z) * 180 / Math.PI);
            p.lightAngleY = (float)(Math.Atan2(p.lightDir.X, p.lightDir.Z) * 180 / Math.PI);

            // 打印光源角度和方向向量
            Console.SetCursorPosition(0, 10); // 设置光标位置，避免覆盖其他内容
            Console.WriteLine($"当前光源角度：X = {p.lightAngleX:F2}°, Y = {p.lightAngleY:F2}°");
            Console.WriteLine($"光源方向向量：({p.lightDir.X:F2}, {p.lightDir.Y:F2}, {p.lightDir.Z:F2})");



        }


    }
    #endregion

}
