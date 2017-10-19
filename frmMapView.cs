using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.GeoAnalyst;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.SpatialAnalyst;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;





namespace LandCheck
{
    public partial class frmMapView : Form
    { 

        #region 数据变量　// 刘扬

        public bool m_bSplit = false; // 不分割

        public IFeature m_theOneFeatureBySplitted = null; // 被分割的第1个Feature
        public IFeature m_theOtherFeatreSplitted = null; // 被分割的第2个Feature


        // 鹰眼
 
        public IElement pMainEle; // 刘扬
   
        // 主窗体
        public mainForm m_pFrmMain = null; // 刘扬

        // 右键菜单
        public bool m_bSelectFeature = true; // 刘扬
        public bool m_bDeleteFeature = false;// 刘扬

        public bool m_bShowStaticsGraph = false; // 刘扬 显示统计图

        // 属性
        ArrayList m_PropertyList = new ArrayList(); // 数据
        ArrayList m_ColNameList = new ArrayList();  // 列名

        private IAoInitialize m_AoInitialize = new AoInitializeClass();

        public IActiveView m_ActiveView;

    

        //打开的mxd文件
        public IMapDocument m_MapDocument;

        #region 地图常数

        private esriUnits m_MapUnits;
        private string m_sMapUnits;

        #endregion

        #endregion 数据变量




        #region  构造函数

        public frmMapView(AxMapControl theControl)
        {
            InitializeComponent();

        }

        public frmMapView(mainForm parentForm)
        {
            InitializeComponent();
            this.m_pFrmMain = parentForm;
            this.m_pFrmMain.m_bIsMapViewFormOpen = true;         

        }


        #endregion 


        #region 自己写的函数

    /////////////////////////////////////////////////////////////////////////////
    /////       　　　　　　　 自己写的函数  （开始）
    /////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// 载入Mxd类型文件到MapControl中
        /// </summary>
        /// <param name="filepath"></param>
        public void LoadFile(string filepath)
        {
            if (this.m_axMapControl.CheckMxFile(filepath))
            {
                this.m_axMapControl.LoadMxFile(filepath);
                this.InitMap();
                //Open document
                OpenDocument(filepath);
            }
        }

        /// <summary>
        /// 打开mapdocument文件
        /// </summary>
        /// <param name="sFilePath"></param>
        private void OpenDocument(string sFilePath)
        {
            //Create a new map document
            m_MapDocument = new MapDocumentClass();
            //Open the map document selected
            m_MapDocument.Open(sFilePath, "");
        }

        /// <summary>
        /// 初始化地图相关参数
        /// </summary>
        private void InitMap()
        {
            this.m_ActiveView = this.m_axMapControl.ActiveView;
            this.m_MapUnits = this.m_axMapControl.MapUnits;
            this.m_sMapUnits = CMapFunction.getMapUnits(this.m_MapUnits);
        }

        /// <summary>
        /// 得到当前地图
        /// </summary>
        /// <returns></returns>
        public IMap GetMap()
        {
            return this.m_axMapControl.ActiveView.FocusMap;
        }

 
        /// <summary>
        /// 得到图层 
        /// </summary>
        /// <param name="pMap"></param>
        /// <returns></returns>
        static public IEnumLayer GetLayers(IMap pMap)
        {
            UID pUid = new UIDClass();
            pUid.Value = "{40A9E885-5533-11d0-98BE-00805F7CED21}";
            return pMap.get_Layers(pUid, true);
        }

  
     
        /// <summary>
        /// 得到编号的Feature对象  
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public IFeature GetFeatureObjectWithCode(string code)
        {
            IActiveView pActiveView = (IActiveView)this.m_axMapControl.ActiveView;

            IFeatureClass pFeatureClass;
            IFeatureCursor pFeatureCursor;
            IFeature pFeature;

            IEnumLayer pLayers = GetLayers(this.m_axMapControl.ActiveView.FocusMap);
            ILayer player = pLayers.Next();
            pFeatureClass = ((IFeatureLayer)player).FeatureClass; ;

            pFeatureCursor = pFeatureClass.Search(null, false);

            pFeature = pFeatureCursor.NextFeature();

            while (pFeature != null)
            {
                int which = pFeature.Fields.FindField("OBJECTID"); // 得到编号

                string str = pFeature.get_Value(0).ToString(); // 获取数据
                if (str.ToLower() == code.ToLower())
                    return pFeature;

                pFeature = pFeatureCursor.NextFeature();
            }

            return null;
        }


        /// <summary>
        /// 放大Feature对象
        /// </summary>
        /// <param name="pFeature"></param>
        public void ZoomToFeature(IFeature pFeature)
        {
            if (pFeature == null)
                return;

            if (pFeature.Shape.GeometryType != esriGeometryType.esriGeometryPoint)
            {
                IActiveView pActiview = (IActiveView)this.m_axMapControl.ActiveView;
                IEnvelope FeaExtent = pActiview.Extent;
                FeaExtent = pFeature.Extent;

                pActiview.Extent = FeaExtent;
                pActiview.Refresh();

                IEnumLayer layers = GetLayers(this.m_axMapControl.ActiveView.FocusMap);
                IFeatureLayer fl = null;
                ILayer layer = layers.Next();
                while (layer != null)
                {
                    fl = (IFeatureLayer)layer;

                    if (fl.FeatureClass == (IFeatureClass)pFeature.Class)
                    {
                        break;
                    }
                    layer = layers.Next();
                }

                IFeatureSelection fs = (IFeatureSelection)fl;

                int index = -1;

                try
                {
                    index = pFeature.Fields.FindField("OBJECTID");
                }
                catch (Exception ex)
                {
                    index = pFeature.Fields.FindField("OBJECTID");
                }

                if (index == -1)
                {
                    return;
                }

                string sql = pFeature.Fields.get_Field(index).Name + " = " + Convert.ToString(pFeature.get_Value(index));


                IQueryFilter qf = new QueryFilterClass();

                qf.WhereClause = sql;

                fs.SelectFeatures(qf, esriSelectionResultEnum.esriSelectionResultNew, true);




            }
            else
            {
                IActiveView pActiview = (IActiveView)this.m_axMapControl.ActiveView;

                IPoint p = new PointClass();
                p = pFeature.Extent.UpperLeft;


                //IEnvelope pExtent = Functions.MapFunction.GetBufferFromAPoint(pActiveview, 3, p.X, p.Y).Envelope;



                IEnvelope pExtent = pFeature.Extent.Envelope;


                //pActiview.Refresh();

                IEnumLayer layers = GetLayers(this.m_axMapControl.ActiveView.FocusMap);
                IFeatureLayer fl = null;
                ILayer layer = layers.Next();
                while (layer != null)
                {
                    fl = (IFeatureLayer)layer;

                    if (fl.FeatureClass == (IFeatureClass)pFeature.Class)
                    {
                        break;
                    }
                    layer = layers.Next();
                }

                IFeatureSelection fs = (IFeatureSelection)fl;

                int index = -1;

                try
                {
                    index = pFeature.Fields.FindField("FID");
                }
                catch (Exception ex)
                {
                    index = pFeature.Fields.FindField("FID");
                }

                if (index == -1)
                {
                    return;
                }

                string sql = pFeature.Fields.get_Field(index).Name + " = " + Convert.ToString(pFeature.get_Value(index));

                IQueryFilter qf = new QueryFilterClass();
                qf.WhereClause = sql;

                fs.SelectFeatures(qf, esriSelectionResultEnum.esriSelectionResultNew, false);


                //IFeatureIdentifyObj pFeatureIdentifyObj = pFeature as IFeatureIdentifyObj;
                //IIdentifyObj pIdObj = pFeatureIdentifyObj as IIdentifyObj;
                //pIdObj.Flash(this.axMapControl1.ActiveView.ScreenDisplay);


                pActiview.Refresh();

                //this.axMapControl1.ActiveView.FocusMap.SelectFeatures(layer, pFeature);

            }



        }


        /// <summary>
        /// 视图设置合适大小
        /// </summary>
        /// <param name="pFeature"></param>
        public void FitView(IFeature pFeature)
        {

            if (pFeature == null)
                return;

            IActiveView pActiview = (IActiveView)this.m_axMapControl.ActiveView;
            IEnvelope pEnvelope = this.m_axMapControl.Extent as IEnvelope;
            pEnvelope.CenterAt(pFeature.Extent.UpperLeft);
            this.m_axMapControl.Extent = pEnvelope;

            pActiview.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
            pActiview.Refresh();
        }



        // 得到选中feature
        public IFeatureCursor GetSelectedFeatures(ILayer theLayer)
        {
            if (theLayer == null) 
                return null;

            // If there are no features selected let the user know
            IFeatureSelection pFeatSel = (IFeatureSelection)theLayer;
            ISelectionSet pSelectionSet = pFeatSel.SelectionSet;
            if (pSelectionSet.Count == 0)
            {
                MessageBox.Show("所操作要素不在 " + theLayer.Name + "层", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }

            // Otherwise get all of the features back from the selection
            ICursor pCursor;
            pSelectionSet.Search(null, false, out pCursor);
            return (IFeatureCursor)pCursor;
        }

        // 得到编辑空间
        public IWorkspaceEdit GetEditWorkspace(IFeatureLayer theLayer)
        {
            if (theLayer == null) 
                return null;

            IFeatureClass fc = theLayer.FeatureClass;

            IFeatureClassWrite fcw = fc as IFeatureClassWrite;

            IWorkspaceEdit w = (fc as IDataset).Workspace as IWorkspaceEdit;

            return w;

        }

        // 划线
        public IGeometry DrawPolyline(ESRI.ArcGIS.Carto.IActiveView activeView)
        {
            if (activeView == null)
            {
                return null;
            }

            ESRI.ArcGIS.Display.IScreenDisplay screenDisplay = activeView.ScreenDisplay;

            // Constant.
            screenDisplay.StartDrawing(screenDisplay.hDC, (System.Int16)ESRI.ArcGIS.Display.esriScreenCache.esriNoScreenCache); // Explicit Cast
            ESRI.ArcGIS.Display.IRgbColor rgbColor = new ESRI.ArcGIS.Display.RgbColorClass();
            rgbColor.Red = 255;

            ESRI.ArcGIS.Display.IColor color = rgbColor; // Implicit cast.
            ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
            simpleLineSymbol.Color = color;

            ESRI.ArcGIS.Display.ISymbol symbol = (ESRI.ArcGIS.Display.ISymbol)simpleLineSymbol; // Explicit cast.
            ESRI.ArcGIS.Display.IRubberBand rubberBand = new ESRI.ArcGIS.Display.RubberLineClass();
            ESRI.ArcGIS.Geometry.IGeometry geometry = rubberBand.TrackNew(screenDisplay, symbol);
            screenDisplay.SetSymbol(symbol);
            screenDisplay.DrawPolyline(geometry);
            screenDisplay.FinishDrawing();

            return geometry;
        }

        // 划线2
        public void DrawPolyline2(ESRI.ArcGIS.Carto.IActiveView activeView, ESRI.ArcGIS.Geometry.IGeometry geometry)
        {
            if (activeView == null || geometry == null)
            {
                return;
            }

            ESRI.ArcGIS.Display.IScreenDisplay screenDisplay = activeView.ScreenDisplay;

            // Constant.
            screenDisplay.StartDrawing(screenDisplay.hDC, (System.Int16)ESRI.ArcGIS.Display.esriScreenCache.esriNoScreenCache); // Explicit Cast
            ESRI.ArcGIS.Display.IRgbColor rgbColor = new ESRI.ArcGIS.Display.RgbColorClass();
            rgbColor.Red = 255;

            ESRI.ArcGIS.Display.IColor color = rgbColor; // Implicit cast.
            ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
            simpleLineSymbol.Color = color;

            ESRI.ArcGIS.Display.ISymbol symbol = (ESRI.ArcGIS.Display.ISymbol)simpleLineSymbol; // Explicit cast.
            ESRI.ArcGIS.Display.IRubberBand rubberBand = new ESRI.ArcGIS.Display.RubberLineClass();

            screenDisplay.SetSymbol(symbol);
            screenDisplay.DrawPolyline(geometry);
            screenDisplay.FinishDrawing();


        }


        // 分割
        public void FeatureSplit2(ISelectionSet selectionSet, ESRI.ArcGIS.Geometry.IPolyline polyline)
        {
            try
            {
                //This function is an example of one way you could split a selected polygon features by a polyline.            
                //open a feature cursor on the selected features that intersect the splitting geometry
                ICursor cursor;
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = polyline;
                selectionSet.Search((IQueryFilter)spatialFilter, false, out cursor);
                IFeatureCursor featureCursor = cursor as IFeatureCursor;
                //clean up the splitting geometry this is nessecary because,         
                //for polygons, IFeatureEdit::Split relies internally on ITopologicalOperator::Cut
                ESRI.ArcGIS.Geometry.ITopologicalOperator topoOpo = (ESRI.ArcGIS.Geometry.ITopologicalOperator)polyline;
                topoOpo.Simplify();        //loop through the features and split them    
                //loop through the features and split them
                IFeatureEdit featureEdit;
                IFeature feature = featureCursor.NextFeature();
                while (feature != null)
                {
                    featureEdit = (IFeatureEdit)feature;
                    ESRI.ArcGIS.esriSystem.ISet set = featureEdit.Split((ESRI.ArcGIS.Geometry.IGeometry)polyline);
                    feature = featureCursor.NextFeature();
                }
            }
            catch (System.Exception errs)
            {
            }


           
        }



        // 分割多边形
        public ESRI.ArcGIS.esriSystem.ISet FeatureSplit(IFeature feature, ESRI.ArcGIS.Geometry.IPolyline polyline)
        {
            try
            {

                ESRI.ArcGIS.Geometry.ITopologicalOperator topoOpo = (ESRI.ArcGIS.Geometry.ITopologicalOperator)polyline;
                topoOpo.Simplify();        //loop through the features and split them    

                IFeatureEdit featureEdit;
                if (feature != null)
                {
                    featureEdit = (IFeatureEdit)feature;
                    ESRI.ArcGIS.esriSystem.ISet set = featureEdit.Split((ESRI.ArcGIS.Geometry.IGeometry)polyline);

                    return set;
                }
            }
            catch (System.Exception errs)
            {
                return null;
            }

            return null;


        }


        // 合并2个多边形
        public IFeature MergePolygonOfTwoFeature(IFeature pFeatureFirst, IFeature pFeatureNext,IWorkspaceEdit m_EditWorkspace)
        {

            try
            {
                if (pFeatureFirst == null || pFeatureNext == null || m_EditWorkspace == null)
                {
                    return null;
                }

                // 开始一个编辑操作，以能够撤销
                m_EditWorkspace.StartEditOperation();

                IGeometry pGeometryFirst = pFeatureFirst.Shape;
                ITopologicalOperator2 topo_oper = (ITopologicalOperator2)pGeometryFirst;

                //ITopologicalOperator的操作是bug很多的，先强制的检查下面三个步骤，再进行操作
                //成功的可能性大一些
                topo_oper.IsKnownSimple_2 = false;
                topo_oper.Simplify();
                pGeometryFirst.SnapToSpatialReference();

                //这是准备合并的图斑使用的
                ITopologicalOperator2 topo_oper2;
                IGeometry pGeometryNext;

                pGeometryNext = pFeatureNext.ShapeCopy;

                //与上面的同理
                topo_oper2 = pGeometryNext as ITopologicalOperator2;
                topo_oper2.IsKnownSimple_2 = false;
                topo_oper2.Simplify();
                pGeometryNext.SnapToSpatialReference();

                //这才是合并图斑的关键
                pGeometryFirst = topo_oper.Union(pGeometryNext);
                pFeatureNext.Delete();

                topo_oper.IsKnownSimple_2 = false;
                topo_oper.Simplify();
                pFeatureFirst.Shape = pGeometryFirst;
                pFeatureFirst.Store();
                m_EditWorkspace.StopEditOperation();

                return pFeatureFirst;
            }
            catch (Exception theErr)
            {
                MessageBox.Show(theErr.ToString(), "错误提示");
                return null;
            }
        }

        // 合并多边形
        public void MergePolygon(IFeatureCursor pFeatureCursor,IWorkspaceEdit m_EditWorkspace)
        {

            if (pFeatureCursor == null || m_EditWorkspace == null)
            {
                MessageBox.Show("无法合并多边形", "提示");
                return;
            }

            IFeature pFeatureFirst = pFeatureCursor.NextFeature();

            if (pFeatureFirst == null)
            {
                MessageBox.Show("无法合并多边形", "提示");
                return;
            }
            // 开始一个编辑操作，以能够撤销
            m_EditWorkspace.StartEditOperation();

            IGeometry pGeometryFirst = pFeatureFirst.Shape;
            ITopologicalOperator2 topo_oper = (ITopologicalOperator2)pGeometryFirst;

            //ITopologicalOperator的操作是bug很多的，先强制的检查下面三个步骤，再进行操作
            //成功的可能性大一些
            topo_oper.IsKnownSimple_2 = false;
            topo_oper.Simplify();
            pGeometryFirst.SnapToSpatialReference();

            //这是准备合并的图斑使用的
            ITopologicalOperator2 topo_oper2;
            IGeometry pGeometryNext;
            IFeature pFeatureNext = pFeatureCursor.NextFeature();

            while (pFeatureNext != null)
            {
                pGeometryNext = pFeatureNext.ShapeCopy;

                //与上面的同理
                topo_oper2 = pGeometryNext as ITopologicalOperator2;
                topo_oper2.IsKnownSimple_2 = false;
                topo_oper2.Simplify();
                pGeometryNext.SnapToSpatialReference();

                //这才是合并图斑的关键
                pGeometryFirst = topo_oper.Union(pGeometryNext);
                pFeatureNext.Delete();

                pFeatureNext = pFeatureCursor.NextFeature();
            }

            topo_oper.IsKnownSimple_2 = false;
            topo_oper.Simplify();
            pFeatureFirst.Shape = pGeometryFirst;
            pFeatureFirst.Store();
            m_EditWorkspace.StopEditOperation();

        }

        private ITable OpenTable(string nameOfFile)
        {
          
            IWorkspaceFactory workspaceFactory = new ESRI.ArcGIS.DataSourcesGDB.AccessWorkspaceFactoryClass();
            IFeatureWorkspace pWorkSpace = workspaceFactory.OpenFromFile(nameOfFile, 0) as IFeatureWorkspace;
            ITable theTable = pWorkSpace.OpenTable("11");
            
            return theTable;

        }


        public static string ParseFieldType(esriFieldType fieldType)
        {

            switch (fieldType)
            {

                case esriFieldType.esriFieldTypeBlob:

                    return "System.String";

                case esriFieldType.esriFieldTypeDate:

                    return "System.DateTime";

                case esriFieldType.esriFieldTypeDouble:

                    return "System.Double";

                case esriFieldType.esriFieldTypeGeometry:

                    return "System.String";

                case esriFieldType.esriFieldTypeGlobalID:

                    return "System.String";

                case esriFieldType.esriFieldTypeGUID:

                    return "System.String";

                case esriFieldType.esriFieldTypeInteger:

                    return "System.Int32";

                case esriFieldType.esriFieldTypeOID:

                    return "System.String";

                case esriFieldType.esriFieldTypeRaster:

                    return "System.String";

                case esriFieldType.esriFieldTypeSingle:

                    return "System.Single";

                case esriFieldType.esriFieldTypeSmallInteger:

                    return "System.Int32";

                case esriFieldType.esriFieldTypeString:

                    return "System.String";

                default:

                    return "System.String";

            }

        }


        private static DataTable CreateDataTableByLayer(ITable pTable, string tableName)
        {

            //创建一个DataTable表

            DataTable pDataTable = new DataTable(tableName);

            //取得ITable接口

            IField pField = null;

            DataColumn pDataColumn;//System.Data.DataTable中列

            //根据每个字段的属性建立DataColumn对象

            for (int i = 0; i < pTable.Fields.FieldCount; i++)
            {

                pField = pTable.Fields.get_Field(i);

                //新建一个DataColumn并设置其属性

                pDataColumn = new DataColumn(pField.Name);

                if (pField.Name == pTable.OIDFieldName)//此列的每行须唯一
                {

                    pDataColumn.Unique = true;//字段值是否唯一

                }

                //字段值是否允许为空

                pDataColumn.AllowDBNull = pField.IsNullable;

                //字段别名

                pDataColumn.Caption = pField.AliasName;

                //字段数据类型

                pDataColumn.DataType = System.Type.GetType(ParseFieldType(pField.Type));

                //字段默认值

                pDataColumn.DefaultValue = pField.DefaultValue;

                //当字段为String类型是设置字段长度

                if (pField.VarType == 8)
                {

                    pDataColumn.MaxLength = pField.Length;

                }

                //字段添加到表中

                pDataTable.Columns.Add(pDataColumn);


            }

            return pDataTable;

        }


        public static DataTable CreateDataTable(ITable pTable, string tableName)
        {

            //创建空DataTable

            DataTable pDataTable = CreateDataTableByLayer(pTable,tableName);

            //创建DataTable的行对象

            DataRow pDataRow = null;

            ICursor pCursor = pTable.Search(null, false);

            //取得ITable中的行信息

            IRow pRow = pCursor.NextRow();

            int n = 0;

            while (pRow != null)
            {

                //新建DataTable的行对象

                pDataRow = pDataTable.NewRow();

                for (int i = 0; i < pRow.Fields.FieldCount; i++)
                {
             
                    pDataRow[i] = pRow.get_Value(i);


                }

                //添加DataRow到DataTable

                pDataTable.Rows.Add(pDataRow);

               pRow = pCursor.NextRow();


            }

            return pDataTable;

        }


        static IRasterDataset OpenFileRasterDataset(string folderName, string datasetName)
        {
            //Open raster file workspace    
            IWorkspaceFactory workspaceFactory = new RasterWorkspaceFactoryClass();
            IRasterWorkspace rasterWorkspace = (IRasterWorkspace)workspaceFactory.OpenFromFile(folderName, 0);
            //Open file raster dataset     
            IRasterDataset rasterDataset = rasterWorkspace.OpenRasterDataset(datasetName); 
            return rasterDataset;
        }



        private void SaveOneFeatureEdit(IFeature theFeature,string fieldName,double value)
        {
     
            if (theFeature == null)
            {
                return;
            }

            IDataset pDataset = (IDataset)theFeature.Class;

            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pDataset.Workspace;

            bool bIsBeingEdited = pWorkspaceEdit.IsBeingEdited();

            if (bIsBeingEdited)
            {
                pWorkspaceEdit.StartEditOperation();
            }
            else
            {
                pWorkspaceEdit.StartEditing(true);
                pWorkspaceEdit.StartEditOperation();
            }

     
            IFields fields = theFeature.Fields;

            int which = fields.FindField(fieldName);

            if (which == -1)
                return;

            IField pField = fields.get_Field(which);

            bool bChecked = pField.CheckValue(value);
            if (bChecked)
            {
                theFeature.set_Value(which, value);
                theFeature.Store();
            }
         
            pWorkspaceEdit.StopEditOperation();
            pWorkspaceEdit.StopEditing(true);

        }


        // 
        public void CalSelectedBandWithFeatureLayer(IFeatureLayer theFlayer, IRasterLayer theRlayer,ArrayList theSelectedBandList)
        {


            STZC.frmProgress theProgress = new STZC.frmProgress(); // 进程条
            int max = 100;
            theProgress.SetMaxValue(max); // 设置最大值
            int current = 5;
            theProgress.SetCurrentValue(current); // 设置当前值
            theProgress.Text = "正在计算(样本时序信息统计)...";
            theProgress.Show();


            // Declare the input value raster object     
            IDataset pDataset = theRlayer as IDataset;

            IWorkspace pWorkSpace = pDataset.Workspace;
            IRasterWorkspace2 pRWorkSpace = pWorkSpace as IRasterWorkspace2;

            IRasterDataset pRasterDataset = pRWorkSpace.OpenRasterDataset(theRlayer.Name);
            IRasterDataset2 pRasterDataset2 = pRasterDataset as IRasterDataset2;

            IRasterBandCollection pRasterBandCollection = pRasterDataset2 as IRasterBandCollection;

            int bandCount = pRasterBandCollection.Count;

            for (int i = 0; i < bandCount; i++)
            {

                current += (int)(95 / bandCount);
                theProgress.SetCurrentValue(current); // 设置当前值

                IRasterBand theBand = pRasterBandCollection.Item(i);
                IGeoDataset theBandDataset = theBand as IGeoDataset;
             
                if (theSelectedBandList.Contains(theBand.Bandname) == true) // 选中的波段才统计
                {
                    string saveFieldName = Globle.CGlobalVarable.m_listNameOfMean[i];
                    CalOneBandWithFeatureLayer(theFlayer, theBandDataset, saveFieldName);
                }

            }

            theProgress.SetCurrentValue(max + 1); // 关闭进程条

            MessageBox.Show("计算完毕！");
        }


        public void CalAllBandWithFeatureLayer(IFeatureLayer theFlayer, IRasterLayer theRlayer)
        {
   
            // Declare the input value raster object     
            IDataset pDataset= theRlayer as IDataset;

            IWorkspace pWorkSpace = pDataset.Workspace;
            IRasterWorkspace2 pRWorkSpace = pWorkSpace as IRasterWorkspace2;

            IRasterDataset pRasterDataset = pRWorkSpace.OpenRasterDataset(theRlayer.Name);
            IRasterDataset2 pRasterDataset2 = pRasterDataset as IRasterDataset2;

            IRasterBandCollection pRasterBandCollection = pRasterDataset2 as IRasterBandCollection;
      
            int bandCount = pRasterBandCollection.Count;

            for (int i = 0; i < bandCount; i++)
            {
                IRasterBand theBand = pRasterBandCollection.Item(i);
                IGeoDataset theBandDataset = theBand as IGeoDataset;
                string saveFieldName = Globle.CGlobalVarable.m_listNameOfMean[i];
                CalOneBandWithFeatureLayer(theFlayer, theBandDataset, saveFieldName);
                
            }


            MessageBox.Show("计算完毕！" );
        }


        public void CalOneBandWithFeatureLayer(IFeatureLayer theFlayer, IGeoDataset theOneBandOfRlayer, string theSaveFieldName)
        {
            try
            {
                // Create the RasterZonalOp object
                IZonalOp pZonalOp = new RasterZonalOpClass();

                // Declare the input zone raster object
                IGeoDataset pZoneRaster = theFlayer.FeatureClass as IGeoDataset;

                // Declare the input value raster object
                IGeoDataset pValueRaster = theOneBandOfRlayer;

                // Declare the output table object
                ITable pOutputTable = pZonalOp.ZonalStatisticsAsTable(pZoneRaster, pValueRaster, true);

                string theIDName = Globle.CGlobalVarable.m_strIDNameOfStatics;
                string theMeanName = Globle.CGlobalVarable.m_strMeanNameOfStatics;

                ICursor pCursor = pOutputTable.Search(null, true);

                IFields pFields = pCursor.Fields;

                int whichValue = pFields.FindField(theIDName);

                if (whichValue == -1)
                    return;

                int whichMean = pFields.FindField(theMeanName);

                if (whichMean == -1)
                    return;
                
                IRow pRow = pCursor.NextRow();

                while (pRow != null)
                {
                    int value = (int)pRow.get_Value(whichValue);
                    float mean = (float)pRow.get_Value(whichMean);

                  
                    // 保存feature
                 

                    IFeature theFeature = CMapFunction.GetOneFeatureByKey(theFlayer, theIDName, value.ToString());
                    SaveOneFeatureEdit(theFeature, theSaveFieldName,(double) mean);

                    pRow = pCursor.NextRow();

                }

        
          
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }


        public IGeoDataset GetOne2()
        {

            // Declare the input value raster object  

            IRasterLayer theRlayer = this.m_axMapControl.get_Layer(2) as IRasterLayer;
            IDataset pDataset = theRlayer as IDataset;

            IWorkspace pWorkSpace = pDataset.Workspace;
            IRasterWorkspace2 pRWorkSpace = pWorkSpace as IRasterWorkspace2;

            IRasterDataset pRasterDataset = pRWorkSpace.OpenRasterDataset(theRlayer.Name);
            IRasterDataset2 pRasterDataset2 = pRasterDataset as IRasterDataset2;

            IRasterBandCollection pRasterBandCollection = pRasterDataset2 as IRasterBandCollection;

            int bandCount = pRasterBandCollection.Count;

            IRasterBand theBand = pRasterBandCollection.Item(0);
            IGeoDataset theBandDataset = theBand as IGeoDataset;


            return theBandDataset;

        }


        public void GetOne()
        {

          

            try
            {


                // Create the RasterZonalOp object
                IZonalOp pZonalOp = new RasterZonalOpClass();

                // Declare the input zone raster object
                IFeatureLayer theFlayer = this.m_axMapControl.get_Layer(0) as FeatureLayer;
                IGeoDataset pZoneRaster=  theFlayer.FeatureClass as IGeoDataset;

                // Declare the input value raster object
                IRasterLayer theRlayer = this.m_axMapControl.get_Layer(2) as IRasterLayer;


                //IGeoDataset pValueRaster= theRlayer.Raster as IGeoDataset;

                //返回theBandDataset;
                IGeoDataset pValueRaster = GetOne2();

                // Declare the output table object
                ITable pOutputTable = pZonalOp.ZonalStatisticsAsTable(pZoneRaster, pValueRaster, true);
           

                frmShowAttributes theForm = new frmShowAttributes();
                theForm.dataGridView1.DataSource = CreateDataTable(pOutputTable, "124");
                theForm.groupBox1.Text = String.Format("共有记录({0})",pOutputTable.RowCount(null));
                theForm.Show();

                //IFeatureClassDescriptor pFeatureClassDescriptor = new FeatureClassDescriptorClass();
                //pFeatureClassDescriptor.Create(Zone, null, "value");
                //IZonalOp pZonalOp = new RasterZonalOpClass();
                //ITable pTable = pZonalOp.ZonalStatisticsAsTable(pFeatureClassDescriptor as IGeoDataset, Value, false);

             
               

                //Geoprocessor gp = new Geoprocessor();
                //gp.OverwriteOutput = true;
                
                
                //ESRI.ArcGIS.SpatialAnalystTools.ZonalStatisticsAsTable theZonal = new ESRI.ArcGIS.SpatialAnalystTools.ZonalStatisticsAsTable();

                //IFeatureLayer theFlayer = this.m_axMapControl.get_Layer(0) as FeatureLayer;
                //theZonal.in_zone_data = theFlayer.FeatureClass as IGeoDataset;

                //IRasterLayer theRlayer = this.m_axMapControl.get_Layer(1) as IRasterLayer;
                //theZonal.in_value_raster = theRlayer.Raster as IGeoDataset;

                //theZonal.ignore_nodata = true;

                //theZonal.zone_field = "value";

                //theZonal.out_table = OpenTable(@"d:\11.mdb") as ITable;

                ////esriGeoAnalysisStatisticsEnum.esriGeoAnalysisStatsMean;
            

                //IGPProcess gpPro = theZonal as IGPProcess;

                //gp.Execute(gpPro, null);
              
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }

        }
        /////////////////////////////////////////////////////////////////////////////
        /////       　　　　　　　 自己写的函数  （结束）
        /////////////////////////////////////////////////////////////////////////////

        #endregion 



        #region 系统事件



        #region MapControl事件

        #region 用到的方法
        // 地图变化
        private void m_axMapControl_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {

        
        }

        // 大小变化
        private void m_axMapControl_OnFullExtentUpdated(object sender, IMapControlEvents2_OnFullExtentUpdatedEvent e)
        {

        }

        // 鼠标移动
        private void m_axMapControl_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            
            #region 状态栏

            double dScale = this.m_axMapControl.MapScale;
            int iScale = (int)dScale;
            string sScale = iScale.ToString();

            //坐标
            this.m_locationLabel.Text = "坐标: " + "X:" + e.mapX.ToString(".0000") + "; Y:" + e.mapY.ToString(".0000") + " " + m_sMapUnits;
            //显示比例
            this.m_scaleLabel.Text = "显示比例: 1:" + sScale;

            #endregion

          
        }


        // 获取一个波段某行列号的像元值
        public object GetValInOneRasterBand(IRasterBand theBand,int rowIndex,int colIndex,int rowNum,int colNum)
        {        

            IRawPixels rawPixels = theBand as IRawPixels;
            IPnt blockSize = new DblPntClass();
            blockSize.SetCoords(1,1);
            IPnt originBlock = new DblPntClass();
            originBlock.X = colIndex;
            originBlock.Y = rowIndex;

            
            IPixelBlock pixelBlock = rawPixels.CreatePixelBlock(blockSize);
            rawPixels.Read(originBlock, pixelBlock);
            object theVal = pixelBlock.GetVal(0, 0, 0);
            return theVal;
           
        }

        // 获取统计图数据
        private ArrayList GetDataOfGraph(string layerName, double mapX, double mapY)
        {
            IRasterLayer theRasterLayer = CMapFunction.GetRasterLayerWithName(layerName, this.m_axMapControl.Map);

            IIdentify Identify;
            Identify = theRasterLayer as IIdentify;

            IArray Array;
            IPoint thePoint = new PointClass();
            thePoint.X = mapX;
            thePoint.Y = mapY;

            Array = Identify.Identify(thePoint);
            
            if (Array != null)
            {
                //IRasterIdentifyObj2 pRasObj;
                //pRasObj = Array.get_Element(0) as IRasterIdentifyObj2;
                //string thePro = "";
                //string theVal = "";
                //pRasObj.GetPropAndValues(0, out thePro, out theVal);
                //MessageBox.Show(String.Format("属性：{0};值：{1}",thePro ,theVal));


                IRaster2 theRaster = theRasterLayer.Raster as IRaster2;

                if (theRaster == null)
                    return null;

                int rowIndex = -1;
                int colIndex = -1;
                theRaster.MapToPixel(mapX, mapY, out colIndex, out rowIndex);

                IRasterBandCollection theRasterBandCollection2 = (IRasterBandCollection)theRasterLayer.Raster;
                int num_band = theRasterBandCollection2.Count;

                IDataset pDataset = theRasterLayer as IDataset;

                IWorkspace pWorkSpace = pDataset.Workspace;
                IRasterWorkspace2 pRWorkSpace = pWorkSpace as IRasterWorkspace2;

                string fileName = System.IO.Path.GetFileName(theRasterLayer.FilePath);
                IRasterDataset pRasterDataset = pRWorkSpace.OpenRasterDataset(fileName);
                IRasterDataset2 pRasterDataset2 = pRasterDataset as IRasterDataset2;

                IRasterBandCollection pRasterBandCollection = pRasterDataset2 as IRasterBandCollection;

                int bandCount = pRasterBandCollection.Count;


                IRasterProps rasterProps = (IRasterProps)theRasterLayer.Raster;

                int dHeight = rasterProps.Height;//当前栅格数据集的行数

                int dWidth = rasterProps.Width; //当前栅格数据集的列数

                ArrayList theList = new ArrayList();

                for (int i = 0; i < bandCount; i++)
                {
                    IRasterBand theBand = pRasterBandCollection.Item(i);
                    object theVal = GetValInOneRasterBand(theBand, rowIndex, colIndex, dHeight, dWidth);
                    theList.Add(theVal);                  

                }

                return theList;

            }

            return null;

        }

        // 分割1次
        public double SplitOnce(IFeature pFeature,IPolyline pSplitLine)
        {
            // 分割后

            // 获得面积即 原始面积
            IArea originArea = pFeature.Shape as IArea; 
            this.m_pFrmMain.ShowTextInConsole(String.Format("原始多边形FID为{0}的面积为：{1}\r\n", pFeature.get_Value(pFeature.Fields.FindField("FID")), originArea.Area)); // 输出控制台显示
            System.Diagnostics.Debug.WriteLine(String.Format("原始多边形FID为{0}的面积为：{1}\r\n", pFeature.get_Value(pFeature.Fields.FindField("FID")), originArea.Area));
            ESRI.ArcGIS.esriSystem.ISet pSet = FeatureSplit(pFeature, pSplitLine);
       

            double min = originArea.Area;

            if (pSet != null)//切割不为空
            {
                IFeature theOne = pSet.Next() as IFeature; 

                //pFeatureSelection.Add(theOne); // 
                //this.m_ActiveView.FocusMap.SelectFeature(pFeatureLayer,theOne);

                double sumArea = 0.0;
                int index = 0;

                this.m_theOneFeatureBySplitted = null; // 清空上一次被切割的2个Feature
                this.m_theOtherFeatreSplitted = null;

                while (theOne != null)//下一次切割
                {
                    IGeometry theShape = theOne.Shape;

                    if (theShape != null)
                    {
                        index++;
                        IArea theArea = theShape as IArea;

                        if (min > theArea.Area)
                            min = theArea.Area;

                        // 每个多边形面积
                        System.Diagnostics.Debug.WriteLine(String.Format("分割后第{0}个FID为{1}的多边形面积为：{2}\r\n", index, int.Parse(theOne.get_Value(theOne.Fields.FindField("FID")).ToString()) - 1, theArea.Area));
                        this.m_pFrmMain.ShowTextInConsole(String.Format("分割后第{0}个FID为{1}的多边形面积为：{2}\r\n", index, int.Parse(theOne.get_Value(theOne.Fields.FindField("FID")).ToString())-1, theArea.Area)); // 输出控制台显示

                        sumArea += theArea.Area; ;
                    }

                    if (index == 1) // 获取第1个Feature
                    {
                        this.m_theOneFeatureBySplitted = theOne;
                    }
                    else if (index == 2) // 获取第2个Feature
                    {
                        this.m_theOtherFeatreSplitted = theOne;
                    }


                    theOne = pSet.Next() as IFeature;//为什么又一遍
                }

            

            }

            return min;
        }


        // 为最后2个Feature设置面积属性 未懂
        public void UpdateAreaFieldOfTheLastTwoFeature()
        {

            // 获取当前图层 -根据图层名得到Feature图层
            ILayer theLayer = CMapFunction.GetFeatureLayerWithName(Globle.CGlobalVarable.m_strCurrentFeatureLayerName, this.m_axMapControl.Map);
            IFeatureLayer pFeatureLayer = theLayer as IFeatureLayer;
            
            // 获取参数1\2
            int count = pFeatureLayer.FeatureClass.FeatureCount(null);
            if (count < 2)
            {
                MessageBox.Show(String.Format("图层：{0}中的Feature数量少于2个", Globle.CGlobalVarable.m_strCurrentFeatureLayerName), "提示");
                return;
            }

            if (this.m_theOneFeatureBySplitted == null || this.m_theOtherFeatreSplitted == null)
            {

                MessageBox.Show("无法合并，至少有一个多边形不存在", "提示");
                return;

            }


            //IFeature pFeatureFisrt = (IFeature)pFeatureLayer.FeatureClass.GetFeature(count - 1);
            //IFeature pFeatureSecond = (IFeature)pFeatureLayer.FeatureClass.GetFeature(count - 2);

            IFeature pFeatureFisrt = this.m_theOneFeatureBySplitted;
            IFeature pFeatureSecond = this.m_theOtherFeatreSplitted;

            
            // 设置后两个的Featrue属性；
            UpdateAreaFieldOfOneFeature(pFeatureFisrt);
            UpdateAreaFieldOfOneFeature(pFeatureSecond);

        }
        
        // 设置面积属性 换算成亩
        public void UpdateAreaFieldOfOneFeature(IFeature pFeatrue)
        {
            if (pFeatrue != null)
            {
                IArea theArea = pFeatrue.Shape as IArea;
                double areaPfm = theArea.Area;
                CMapFunction.SetFeatureAttribute(pFeatrue, "mj", areaPfm.ToString("0.00"), this.m_axMapControl, Globle.CGlobalVarable.m_strCurrentFeatureLayerName);
                double areaMu = areaPfm / 666.67;
                CMapFunction.SetFeatureAttribute(pFeatrue, "亩", areaMu.ToString("0.00"), this.m_axMapControl, Globle.CGlobalVarable.m_strCurrentFeatureLayerName);
            }

        }


        // 添加一个元素 画蓝方块
        public void AddRectangleElement(IEnvelope pNewMainEnv)
        {
            

            IGraphicsContainer pGraphCon = this.m_axMapControl.Map as IGraphicsContainer;
            IActiveView pActiveView = pGraphCon as IActiveView;
            pGraphCon.DeleteAllElements();

            IRectangleElement pRectEle = new RectangleElementClass();
            IElement pEle = pRectEle as IElement;
            pEle.Geometry = pNewMainEnv as IGeometry;

            IRgbColor pColor = new RgbColorClass();
            pColor.Red = 0;
            pColor.Green = 0;
            pColor.Blue = 255;
            pColor.Transparency = 225;

            ISimpleLineSymbol pOutline = new SimpleLineSymbolClass();
    
            pOutline.Color = pColor;
            pOutline.Style = esriSimpleLineStyle.esriSLSDashDotDot;
            pOutline.Width = 2;
          

            pColor = new RgbColorClass();
            pColor.Red = 225;
            pColor.Green = 0;
            pColor.Blue = 0;
            pColor.Transparency = 0;

            IFillSymbol pFillSymbol = new SimpleFillSymbolClass();
            pFillSymbol.Color = pColor;
            pFillSymbol.Outline = pOutline;

            IFillShapeElement pFillShapeEle = pEle as IFillShapeElement;
            pFillShapeEle.Symbol = pFillSymbol;

            pGraphCon.AddElement((IElement)pFillShapeEle, 0);

            pActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);  

        }

        //==============================李思琦 改动start=======================================   
        // 延长线
        public IPolyline GetExtendLine(IPolyline passLine, double dis1, double dis2)
        {
            IPointCollection pPointCol = passLine as IPointCollection;
            IPoint fPoint = new PointClass();
            IPoint ePoint = new PointClass();
            object missing2 = Type.Missing;
            //左
            passLine.QueryPoint(esriSegmentExtension.esriExtendAtFrom, -1 * dis1, false, fPoint);
            pPointCol.InsertPoints(0, 1, ref fPoint);
            //右        
            passLine.QueryPoint(esriSegmentExtension.esriExtendAtTo,  passLine.Length-dis2, false, ePoint);
            //Console.WriteLine("延长端点" + ePoint.X);
            pPointCol.AddPoint(ePoint, ref missing2, ref missing2);
                     
            return pPointCol as IPolyline;
        }

        private IPolyline BreakLineToTwoPart(IPolyline myPolyline, double DisOnLine)
        {
            if (DisOnLine > myPolyline.Length)        //如果传入的长度大于线的长度，不与操作
            {
                return null;
            }
            //IPolyline[] Lines = new IPolyline[2];
            bool isSplit;
            int splitIndex, segIndex;
            object o = Type.Missing;
            myPolyline.SplitAtDistance(DisOnLine, false, false, out isSplit, out splitIndex, out segIndex);
            IPolyline newLine = new PolylineClass();
            ISegmentCollection lineSegCol = (ISegmentCollection)myPolyline;
            ISegmentCollection newSegCol = (ISegmentCollection)newLine;
            for (int j = segIndex; j < lineSegCol.SegmentCount; j++)
            {
                newSegCol.AddSegment(lineSegCol.get_Segment(j), ref o, ref o);
            }
            //重新构建两条线
            lineSegCol.RemoveSegments(segIndex, lineSegCol.SegmentCount - segIndex, true);
            lineSegCol.SegmentsChanged();
            newSegCol.SegmentsChanged();
            IPolyline oldLine = lineSegCol as IPolyline;
            newLine = newSegCol as IPolyline;

            return newLine;
        }

        public void MoveLineWithStartPoint(IPolyline line, IPoint p1, out IPolyline theline, out IPoint PointStart, out IPoint PointEnd, out IPoint movedestination)
        {
            IPointCollection PointCollection = line as IPointCollection;
            int PointCount = PointCollection.PointCount;
            //Console.WriteLine( "端点个数" + linePointCount);
            PointStart = PointCollection.get_Point(0);//起点
            PointEnd = PointCollection.get_Point(PointCount-1);//终点
            IPoint PointMinY = PointStart;
            for (int i=0; i < PointCount; i++)
            {

                if (PointMinY.Y > PointCollection.get_Point(i).Y)
                    PointMinY = PointCollection.get_Point(i);
            }
            //Console.WriteLine("原来端点" + linePointEnd.X);
            //Console.WriteLine(linePointStart.X + "linePointStart" + linePointEnd.X);
                
            IMoveLineFeedback m_MoveLineFeedback = new MoveLineFeedbackClass();
            movedestination = PointStart;
            m_MoveLineFeedback = new MoveLineFeedbackClass();
            m_MoveLineFeedback.Start(line, PointStart);
            //movedestination.X = p1.X;
            movedestination.Y = p1.Y-(PointStart.Y-PointMinY.Y)-1;
            m_MoveLineFeedback.MoveTo(movedestination);
            theline = (IPolyline)m_MoveLineFeedback.Stop();
            //return line;
            //DrawPolyline2(this.m_axMapControl.ActiveView, line as IGeometry); // 画出平行线 

        }

        public double getMinYandMaxY(IPolyline theline,int N)
        {
            
            IPointCollection pPointCollection;
            pPointCollection = theline as IPointCollection;

            int n = pPointCollection.PointCount;
            double[] coordY = new double[n];
            double[] coordX = new double[n];


            for (int i = 0; i < n; i++)
            {
                IPoint point = pPointCollection.get_Point(i);
                coordX[i] = point.X;
                coordY[i] = point.Y;
            }
            //对数组进行从小到大排序
            System.Array.Sort(coordY);

            double Ycoord;
            if (N == 0) Ycoord = coordY[0];
            else Ycoord = coordY[n - 1];
            return Ycoord;
        }

        public IPoint getMinYandMaxY2(IPolyline theline, int N)
        {

            IPointCollection linePointCollection = theline as IPointCollection;
            int linePointCount = linePointCollection.PointCount;
           
            IPoint linePointMax = linePointCollection.get_Point(0);//起点
            IPoint linePointMin = linePointCollection.get_Point(linePointCount - 1);//终点
            
            for (int i = 0; i < linePointCount; i++)
            {

                if (linePointMin.Y > linePointCollection.get_Point(i).Y)
                    linePointMin = linePointCollection.get_Point(i);
                if (linePointMax.Y < linePointCollection.get_Point(i).Y)
                    linePointMax = linePointCollection.get_Point(i);

            }
            IPoint Ycoord;
            if (N == 0) Ycoord = linePointMin;
            else Ycoord = linePointMax;
            return Ycoord;
        }

        private void AddField(IFeatureClass pFeatureClass, string name, string aliasName, esriFieldType FieldType)
        {
            //若存在，则不需添加
            if (pFeatureClass.Fields.FindField(name) > -1) return;
            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = pField as IFieldEdit;
            pFieldEdit.AliasName_2 = aliasName;
            pFieldEdit.Name_2 = name;
            pFieldEdit.Type_2 = FieldType;

            IClass pClass = pFeatureClass as IClass;
            pClass.AddField(pField);
        }

        //++++++++++++++++++++++++++++++李思琦 改动end+++++++++++++++++++++++++++++++++++++++++
        #endregion

        #region   MapControl 鼠标按下
        /// <summary>
        /// MapControl相关事件——鼠标按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_axMapControl_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            IActiveView pActiveView = this.m_axMapControl.ActiveView;
            List<IPoint> pointlist = new List<IPoint>();
            //List<double> lineY = new List<double>();
            List<double> offse = new List<double>();
            List<double> lineYmax = new List<double>();
            List<double> lineYmin = new List<double>();
            int linecount=0;

            if (this.m_bSplit)
            //---if (this.m_bSplit && Globle.CGlobalVarable.m_strCurrentFeatureLayerName != "")
            {
                //设置当前工具和鼠标样式
                this.m_axMapControl.CurrentTool = null; 
                this.m_axMapControl.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
               
                // 设置多边形划分为false
                // 显示分割目标（面积）
                //this.m_pFrmMain.ShowTextInConsole(String.Format("分割目标：{0}和{1}\r\n", Globle.CGlobalVarable.g_dSplittedPlogyonAreaFirst,Globle.CGlobalVarable.g_dSplittedPlogyonAreaSecond)); // 输出控制台显示

             
                //--this.m_bSplit = false;
                //--ILayer m_CurrentLayer = CMapFunction.GetFeatureLayerWithName(Globle.CGlobalVarable.m_strCurrentFeatureLayerName, this.m_axMapControl.Map);
                //--IFeatureCursor featureCursor = GetSelectedFeatures(m_CurrentLayer);
                //--IFeature feature = featureCursor.NextFeature();

                ILayer lyr = m_axMapControl.Map.get_Layer(0);
                IFeatureLayer featurelyr = (IGeoFeatureLayer)lyr;

              

                IQueryFilter ppQueryFilter = new QueryFilterClass();
                IFeatureCursor pFeatureCursor = null;

                ppQueryFilter.SubFields = "FID";
                pFeatureCursor = featurelyr.FeatureClass.Search(ppQueryFilter, true);

                IDataStatistics pDataStati = new DataStatisticsClass();
                pDataStati.Field = "FID";
                pDataStati.Cursor = (ICursor)pFeatureCursor;
                
                IFeatureClass fcls = featurelyr.FeatureClass;
                AddField(fcls, "featureID", "fid", esriFieldType.esriFieldTypeInteger);

                IEnumerator pEnumerator = pDataStati.UniqueValues;
                pEnumerator.Reset();
                ArrayList featureFID = new ArrayList();
                int fid = 0;
                while (pEnumerator.MoveNext())
                {
                    object pObj = pEnumerator.Current;

                    if (fid == int.Parse(pObj.ToString()))
                    {
                        IFeature pFeature = fcls.GetFeature(fid);
                        pFeature.set_Value(pFeature.Fields.FindField("featureID"), fid);   //每个要素的“A”字段存储的都是“B”。
                        pFeature.Store();
                    }



                   
                    fid++;
                    Console.WriteLine("fid" + fid);

                }

                
                string sql = null;
               
                sql = "featureID = " + 0;
                IQueryFilter pQueryFilter = new QueryFilterClass();
             
                pQueryFilter.WhereClause = sql;
                pFeatureCursor = fcls.Search(pQueryFilter, true);
                IFeature feature = pFeatureCursor.NextFeature();
                




                if (feature == null)
                {
                    MessageBox.Show("选中对象不存在，无法执行分割","提示");
                    return;
                }
                else m_axMapControl.FlashShape(feature.Shape);
               

                IGeometry Line = DrawPolyline(this.m_axMapControl.ActiveView);
                IPolyline line = Line as IPolyline;
                IPolyline theline;
                IPointCollection linePointCollection = Line as IPointCollection;
                int linePointCount = linePointCollection.PointCount;
                //Console.WriteLine( "端点个数" + linePointCount);
                IPoint linePointStart = linePointCollection.get_Point(0);//起点
                IPoint linePointEnd = linePointCollection.get_Point(linePointCount - 1);//终点
                IPoint linePointMinY = linePointStart;
                for (int i = 0; i < linePointCount; i++)
                {

                    if (linePointMinY.Y > linePointCollection.get_Point(i).Y)
                        linePointMinY = linePointCollection.get_Point(i);
                }
                int spiltcount = 0;
                #region 自动化循环
                while (feature != null)
                {
                    spiltcount++;
                    //featurecount++;
                    Console.WriteLine("开始第" + spiltcount + "个要素切割");
                    IEnvelope pEnvelope = feature.Extent;//(为了放大)

                    //获取蓝框坐标
                    IPoint p1 = new PointClass();
                    IPoint p2 = new PointClass();
                    IPoint p3 = new PointClass();
                    IPoint p4 = new PointClass();
                    if (pEnvelope != null && !pEnvelope.IsEmpty)
                    {


                        if (pEnvelope == null)
                            return;

                        p1 = pEnvelope.UpperLeft;
                        p2 = pEnvelope.LowerLeft;
                        p3 = pEnvelope.LowerRight;
                        p4 = pEnvelope.UpperRight;
                        IPointCollection bluePointCollection = new PolygonClass();
                        object missing = Type.Missing;
                        bluePointCollection.AddPoint(p1, ref missing, ref missing);
                        bluePointCollection.AddPoint(p2, ref missing, ref missing);
                        bluePointCollection.AddPoint(p3, ref missing, ref missing);
                        bluePointCollection.AddPoint(p4, ref missing, ref missing);
                        //Console.WriteLine("蓝框左"+p1.X +"蓝框右"+ p4.X);

                    }

                    //获取画好线（line）的坐标



                    IMoveLineFeedback m_MoveLineFeedback = new MoveLineFeedbackClass();
                    IPoint movedestination = linePointMinY;
                    m_MoveLineFeedback = new MoveLineFeedbackClass();
                    m_MoveLineFeedback.Start(line, linePointMinY);
                    //movedestination.X = p1.X;
                    //movedestination.Y = p1.Y + (linePointStart.Y - linePointMinY.Y) ;
                    movedestination.Y = p1.Y;
                    m_MoveLineFeedback.MoveTo(movedestination);
                    line = (IPolyline)m_MoveLineFeedback.Stop();




                    double startdifference = linePointStart.X - p1.X;
                    double enddifference = linePointEnd.X - p4.X;
                    //Console.WriteLine(startdifference + "---" + enddifference);

                    #region 裁剪与延长
                    theline = line;




                    IEnvelope cutEnvelope = pEnvelope;
                    //cutEnvelope.PutCoords(pEnvelope.XMin, pEnvelope.YMin, pEnvelope.XMax, pEnvelope.YMax);
                    cutEnvelope.Expand(1, 5, true);
                    ITopologicalOperator pTopoOperator = theline as ITopologicalOperator;
                    pTopoOperator.Clip(cutEnvelope);


                    if (startdifference > 0 && enddifference >= 0)
                    {
                        theline = GetExtendLine(theline, startdifference, 0);
                    }
                    if (startdifference > 0 && enddifference < 0)//左短右短
                    {
                        theline = GetExtendLine(line, startdifference, enddifference);
                    }

                    if (startdifference == 0 && enddifference < 0)//右短
                    {
                        theline = GetExtendLine(theline, 0, enddifference);
                    }
                    if (startdifference < 0 && enddifference < 0)//左长右短
                    {
                        theline = GetExtendLine(line, startdifference, enddifference);
                    }
                    #endregion

                    //获取裁剪、延长后线（theline）坐标
                    //IPointCollection thelinePointCollection = (IPointCollection)theline;
                    //=============================
                    IPointCollection thelinePointCollection = theline as IPointCollection;
                    int thelinePointCount = thelinePointCollection.PointCount;
                    IPoint thelinePointStart = thelinePointCollection.get_Point(0);//起点
                    IPoint thelinePointEnd = thelinePointCollection.get_Point(thelinePointCount - 1);//终点
                    //movedestination = thelinePointStart;
                    Console.WriteLine(thelinePointStart.X + "~~起点~~~" + p1.X);
                    Console.WriteLine(thelinePointEnd.X + "~~~终点~~" + p4.X);
                    Console.WriteLine("~~~" + p1.Y);
                    Console.WriteLine("~==~~" + p2.Y);


                    linePointMinY = thelinePointStart;
                    IPoint linePointMaxY = thelinePointStart;
                    //得到theline的最高点与最低点
                    for (int i = 0; i < thelinePointCount; i++)
                    {

                        if (linePointMinY.Y > thelinePointCollection.get_Point(i).Y)
                            linePointMinY = thelinePointCollection.get_Point(i);
                        if (linePointMaxY.Y < thelinePointCollection.get_Point(i).Y)
                            linePointMaxY = thelinePointCollection.get_Point(i);
                    }
                    //===================================




                    m_MoveLineFeedback.Start(theline, linePointMinY);
                    movedestination.X = linePointMinY.X;
                    //movedestination.Y = p1.Y - Globle.CGlobalVarable.g_dParallelLinesInterval;
                    movedestination.Y = p1.Y;

                    m_MoveLineFeedback.MoveTo(movedestination);
                    theline = (IPolyline)m_MoveLineFeedback.Stop();
                    DrawPolyline2(this.m_axMapControl.ActiveView, theline as IGeometry); // 画出平行线





                    //IConstructCurve mycurve = new PolylineClass();

                    //++++++++++++++++++++++++++++++李思琦 改动end+++++++++++++++++++++++++++++++++++++++++

                    //平行线的间隔 
                    double bufferDis = Globle.CGlobalVarable.g_dParallelLinesInterval;
                    ArrayList theOffsetBaseList = new ArrayList(); // 保存历史偏移的数据
                    int index = 1;
                    double theOffsetBase = bufferDis;// 偏移绝对值
                    theOffsetBaseList.Add(theOffsetBase);// 保存每次的偏移绝对值

                    //mycurve.ConstructOffset(theline, theOffsetBase);
                    //object o = System.Type.Missing;

                    //构造偏移点 https://wenku.baidu.com/view/25f170dc5022aaea998f0f44.html
                    //mycurve.ConstructOffset(theline, theOffsetBase, ref o, ref o);

                    theOffsetBaseList.Add(bufferDis * index);// 保存历史偏移的数据
                    //DrawPolyline2(this.m_axMapControl.ActiveView, line as IGeometry);


                    // 分割1次多边形
                    this.m_pFrmMain.ShowTextInConsole(String.Format("第{0}次切割\r\n", index)); // 输出控制台显示
                    double currentMinArea;
                    //==============================李思琦 改动start=======================================   
                    IArea originArea = feature.Shape as IArea;

                    movedestination = thelinePointStart;
                    do
                    {
                        currentMinArea = SplitOnce(feature, theline);
                        // 切割前后最小面积的间隔（差）    
                        Console.WriteLine("第" + index + "切割完成，间隔" + theOffsetBase + "\r\n本次面积" + currentMinArea);
                        m_MoveLineFeedback.Start(theline, movedestination);
                        //movedestination.X = thelinePointStart.X;
                        movedestination.Y = movedestination.Y - theOffsetBase;
                        m_MoveLineFeedback.MoveTo(movedestination);
                        theline = (IPolyline)m_MoveLineFeedback.Stop();
                        //将线的最小值与最大值存入数组
                        //lineYmax.Add(getMinYandMaxY(theline, 1));
                        //lineYmin.Add(getMinYandMaxY(theline, 0));
                        linePointMinY = getMinYandMaxY2(theline, 0);
                        linePointMaxY = getMinYandMaxY2(theline, 1);
                        //Console.WriteLine("lineYmax[" + linecount + "]:" + lineYmax[linecount] + "            lineY[" + linecount + "]:" + lineYmin[linecount]);
                        Console.WriteLine("lineYmax: " + linePointMaxY.Y + "            lineYmin: " + linePointMinY.Y);
                        linecount++;
                    }
                    while (currentMinArea == originArea.Area);


                    //++++++++++++++++++++++++++++++李思琦 改动end+++++++++++++++++++++++++++++++++++++++++
                    DrawPolyline2(this.m_axMapControl.ActiveView, theline as IGeometry); // 画出平行线
                    //Console.WriteLine("面积" + currentMinArea);



                    // 当前误差-必须不断变小
                    double currentError = Math.Abs(currentMinArea - Globle.CGlobalVarable.g_dSplittedPlogyonAreaMin);


                    double theLastMinArea = currentMinArea; // 获取上一次的最小面积（偏移平行线）
                    double theLastOffset = theOffsetBase;// 上一次的偏移

                    bool splitSuccessed = true;
                    double speed = 0.0;

                    // 绝对值大于误差
                    while (currentError > Globle.CGlobalVarable.g_dMaxError)
                    {
                        index++;
                        // 合并最后2个多边形
                        feature = this.m_pFrmMain.MergePolygonOfTTheLastTwoFeature();



                        if (feature == null)
                        {

                            this.m_pFrmMain.ShowTextInConsole("错误：无法继续分割，因为合并对变形失败！"); // 输出控制台显示

                            return;

                        }




                        // 分割1次多边形
                        this.m_pFrmMain.ShowTextInConsole(String.Format("第{0}次切割\r\n", index)); // 输出控制台显示
                        Console.WriteLine("第{0}次切割\r\n", index);
                        currentMinArea = SplitOnce(feature, theline);
                        Console.WriteLine("第" + index + "切割完成，间隔" + theOffsetBase + "\r\n上一次面积" + theLastMinArea + "\r\n本次面积" + currentMinArea);
                        //if (currentMinArea == originArea.Area)
                        //{
                        //    //currentMinArea = SplitOnce(feature, theline);
                        //    // 切割前后最小面积的间隔（差）    
                        //    Console.WriteLine("超过切割范围！");
                        //    m_MoveLineFeedback.Start(theline, movedestination);
                        //    //movedestination.X = thelinePointStart.X;
                        //    movedestination.Y = movedestination.Y + 10;
                        //    m_MoveLineFeedback.MoveTo(movedestination);
                        //    theline = (IPolyline)m_MoveLineFeedback.Stop();

                        //}
                        double areaInterval = Math.Abs(theLastMinArea - currentMinArea); // 切割前后最小面积的间隔（差）
                        //==============================李思琦 改动start=======================================
                        speed = (Globle.CGlobalVarable.g_dSplittedPlogyonAreaMin - currentMinArea) / (currentMinArea - theLastMinArea);
                        speed = Math.Round(speed, 2);
                        Console.WriteLine("第" + index + "切割完成，速率" + speed);

                        //++++++++++++++++++++++++++++++李思琦 改动end+++++++++++++++++++++++++++++++++++++++++


                        theLastMinArea = currentMinArea;// 获取上一次的最小面积（偏移平行线）
                        // 判断当前误差是否超过最大控制误差
                        if (currentError > Math.Abs(currentMinArea - Globle.CGlobalVarable.g_dSplittedPlogyonAreaMin))
                        {
                            currentError = Math.Abs(currentMinArea - Globle.CGlobalVarable.g_dSplittedPlogyonAreaMin);// 当前误差
                            this.m_pFrmMain.ShowTextInConsole(String.Format("当前误差为：{0},速率为{1}\r\n", currentError, speed)); // 输出控制台显示
                        }
                        //==============================李思琦 改动start=======================================      
                        //else
                        //{

                        //    this.m_pFrmMain.ShowTextInConsole(String.Format("误差扩大为：{0}；速率为{1},无法达到设置的最大控制误差{2}，请重新分割。\r\n", Math.Abs(currentMinArea - Globle.CGlobalVarable.g_dSplittedPlogyonAreaMin),speed, Globle.CGlobalVarable.g_dMaxError)); // 输出控制台显示
                        //    splitSuccessed = false;
                        //    break; // 当前误差
                        //}


                        #region 智能化的移动平行线


                        double offset = 0.0; // 偏移相对值
                        if (speed >= 1) // 
                        {

                            offset = theLastOffset * Math.Ceiling(System.Math.Sqrt(speed));

                        }
                        else if (speed < 1 && speed > 0) // 
                        {
                            offset = (theLastOffset / Math.Ceiling(System.Math.Sqrt(1 / speed)));

                        }
                        else if (speed < 0 && speed > -1) // 
                        {
                            offset = (-1) * theLastOffset / Math.Ceiling(System.Math.Sqrt(1 / Math.Abs(speed)));

                        }
                        else if (speed <= -1) // 
                        {
                            offset = theLastOffset * (-1);

                        }
                        else // 正常
                        {
                            offset = theLastOffset;

                        }
                        if (Math.Abs(offset) > (p1.Y - p4.Y) / 2)
                        {
                            offset = offset / 2;
                        }
                        #endregion


                        //++++++++++++++++++++++++++++++李思琦 改动end+++++++++++++++++++++++++++++++++++++++++


                        //this.m_pFrmMain.ShowTextInConsole(String.Format("currentError:{0};areaInterval:{1};offset：{2}\r\n",currentError,areaInterval,offset)); // 输出控制台显示
                        offset = Math.Round(offset, 2);
                        theOffsetBase += offset; // 偏移绝对值
                        theLastOffset = offset; // 获取当前偏移
                        theOffsetBaseList.Add(theOffsetBase);// 保存每次的偏移绝对值

                        //==============================李思琦 改动start=======================================      
                        // 移动一次平行线   
                        m_MoveLineFeedback.Start(theline, movedestination);
                        movedestination.Y = movedestination.Y - offset;
                        m_MoveLineFeedback.MoveTo(movedestination);
                        theline = m_MoveLineFeedback.Stop();
                        //lineYmax.Add(getMinYandMaxY(theline, 1));
                        //lineYmin.Add(getMinYandMaxY(theline, 0));
                        linePointMinY = getMinYandMaxY2(theline, 0);
                        linePointMaxY = getMinYandMaxY2(theline, 1);
                        //Console.WriteLine("lineYmax[" + linecount + "]:" + lineYmax[linecount] + "            lineY[" + linecount + "]:" + lineYmin[linecount]);
                        Console.WriteLine("lineYmax: " + linePointMaxY.Y + "            lineYmin: " + linePointMinY.Y);
                        linecount++;
                        Console.WriteLine("第" + index + "切割完成后，移动" + offset);
                        //Console.WriteLine("线最大端的X" + linePointMaxY.X + "线最小端的X" + linePointMinY.X);



                        //超出则回滚
                        if (linePointMinY.Y < p2.Y)
                        {
                            Console.WriteLine("linePointMinY.Y" + linePointMinY.Y + " < p2.Y" + p2.Y);
                            m_MoveLineFeedback.Start(theline, linePointMinY);
                            linePointMinY.Y = p2.Y + 1;
                            m_MoveLineFeedback.MoveTo(linePointMinY);
                            theline = m_MoveLineFeedback.Stop();
                            Console.WriteLine("可能超出边界，回滚到下界" + linePointMinY.Y);
                        }
                        else if (linePointMaxY.Y > p1.Y)
                        {
                            Console.WriteLine("linePointMaxY.Y" + linePointMaxY.Y + "> p1.Y" + p1.Y);
                            m_MoveLineFeedback.Start(theline, linePointMaxY);
                            linePointMaxY.Y = p1.Y - 1;
                            m_MoveLineFeedback.MoveTo(linePointMaxY);
                            theline = m_MoveLineFeedback.Stop();
                            Console.WriteLine("可能超出边界，回滚到上界" + linePointMaxY.Y);
                        }

                        //++++++++++++++++++++++++++++++李思琦 改动end+++++++++++++++++++++++++++++++++++++++++
                        DrawPolyline2(this.m_axMapControl.ActiveView, theline as IGeometry); // 画出平行线


                        System.Diagnostics.Debug.WriteLine(index.ToString());

                    }
                    // 显示分割目标
                    this.m_pFrmMain.ShowTextInConsole(String.Format("分割目标：{0}和{1}\r\n", Globle.CGlobalVarable.g_dSplittedPlogyonAreaFirst, Globle.CGlobalVarable.g_dSplittedPlogyonAreaSecond)); // 输出控制台显示


                    // 显示实际分割结果
                    double totalArea = Globle.CGlobalVarable.g_dSplittedPlogyonAreaFirst + Globle.CGlobalVarable.g_dSplittedPlogyonAreaSecond;
                    this.m_pFrmMain.ShowTextInConsole(String.Format("实际分割后为：{0}和{1}\r\n", currentMinArea, totalArea - currentMinArea)); // 输出控制台显示










                    if (splitSuccessed == true) // 分割成功
                    {
                        this.m_pFrmMain.ShowTextInConsole(String.Format("分割操作成功")); // 输出控制台显示
                        //this.m_axMapControl.Map.ClearSelection();
                        //this.m_axMapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);

                        pEnvelope.Expand(1, 0.2, true);
                        this.m_axMapControl.ActiveView.Extent = pEnvelope;// 为了放大)
                        AddRectangleElement(pEnvelope);

                    }
                    else
                        this.m_pFrmMain.ShowTextInConsole(String.Format("分割操作失败")); // 输出控制台显示

                    this.m_pFrmMain.ShowTextInConsole(String.Format("分割操作结束！\r\n")); // 输出控制台显示

                    // 为最后2个Feature设置面积属性
                    UpdateAreaFieldOfTheLastTwoFeature();


                    //设置鼠标样式
                    this.m_axMapControl.MousePointer = esriControlsMousePointer.esriPointerArrow;
                    this.m_axMapControl.ActiveView.Refresh();

                    sql = "featureID = " + spiltcount;
                    pQueryFilter.WhereClause = sql;
                    pFeatureCursor = fcls.Search(pQueryFilter, true);
                    feature = pFeatureCursor.NextFeature();
                    //feature = fcls.GetFeature(featurefid[featurecount]);

                    if (feature == null)
                        Console.WriteLine("下一个feature为空");
                }
                #endregion

            }
            else if(this.m_bShowStaticsGraph == true)
            {
                int count = Globle.CGlobalVarable.m_theTitleListOfShowStaticsGraph.Count;

                // 获取数据
                ArrayList theDataList = new ArrayList();
                Globle.CGlobalVarable.m_nMaxOfBandNum = 10; // 最小为10
                for (int i = 0; i < count; i++)
                {
                    string layerName = Globle.CGlobalVarable.m_theTitleListOfShowStaticsGraph[i].ToString();
                    ArrayList theData = GetDataOfGraph(layerName,e.mapX,e.mapY);
                    theDataList.Add(theData);

                    if(theData!=null)
                    {
                        if (Globle.CGlobalVarable.m_nMaxOfBandNum < theData.Count)
                            Globle.CGlobalVarable.m_nMaxOfBandNum = theData.Count;
                    
                    }
                }


                // 没有关闭显示样本选择的窗体
                if (Globle.CGlobalVarable.m_thefrmOfShowServeralStaticsGraph != null)
                {
                    Globle.CGlobalVarable.m_thefrmOfShowServeralStaticsGraph.Text = String.Format("多波段统计图[位置({0},{1})]", e.mapX, e.mapY);
                    Globle.CGlobalVarable.m_thefrmOfShowServeralStaticsGraph.ShowAllStaticsGraph(theDataList);

                    if (Globle.CGlobalVarable.m_thefrmOfShowServeralStaticsGraph.WindowState == FormWindowState.Minimized)
                    {
                        Globle.CGlobalVarable.m_thefrmOfShowServeralStaticsGraph.WindowState = FormWindowState.Normal;
                    }


                    Globle.CGlobalVarable.m_thefrmOfShowServeralStaticsGraph.Activate();
                }
                else
                {
                    frmShowSeveralGraph theForm = new frmShowSeveralGraph(theDataList);
                    Globle.CGlobalVarable.m_thefrmOfShowServeralStaticsGraph = theForm;
                    theForm.Text = String.Format("多波段统计图[位置({0},{1})]", e.mapX, e.mapY);
                    theForm.Show();
                }

                
            }

        }

        #endregion 


        #endregion


        #region 窗体事件

        // 窗体初始化
        private void frmMapView_Load(object sender, EventArgs e)
        {

            //this.LoadFile(Application.StartupPath + @"\图层.mxd");
          
            this.WindowState = FormWindowState.Maximized;

               

        }

        // 窗体关闭
        private void frmMapView_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (this.m_pFrmMain.m_bIsEdit)
            //{
            //    MessageBox.Show("请先关闭编辑工具条！");
            //    e.Cancel = true;

            //    return;
            //}

            //if (this.m_axMapControl != null)
            //{
            //    this.m_axMapControl.ClearLayers();
            //}

            //this.m_pFrmMain.m_bIsMapViewFormOpen = false;
            //this.m_pFrmMain.m_bIsFirstStart = false;
            //this.m_pFrmMain.m_axTOCControl.ActiveView.Clear();

            m_AoInitialize.Shutdown();
        }

        #endregion

      

        #endregion




    }
}