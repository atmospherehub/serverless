# Gateway for Atmosphere stations and API
```
                                                       +-queue: atmosphere-processed-images
                                                       |
                                                       ?
                                                       |
+--------------------+        +--------------------+   |    +--------------------+
|    FacesUpload     +--------+    ProcessImage    +---+---->  PostProcessImage  |
+--------------------+        +--------------------+        +---------+----------+   +-topic: atmosphere-images-with-faces
                                                                      |              |
                                                                      +-------?------+
                                                                      |
                         +--------------+--------------+--------------+--------------+
                         |              |              |                             |
                         |              |    +---------v----------+        +---------v----------+
                         |              |    |     StoreTable     |        |     StoreSql       |
                         |              |    +--------------------+        +---------+----------+
                         |              |                                            |
                         |              |                                            |
                         |    +---------v----------+                                 |
                         |    |    NotifySlack     |                       +---------v----------+
                         |    +--------------------+                       |   StoreRectangles  |
                         |                                                 +--------------------+
                         |
               +---------v----------+
               |     NotifyMQTT     |
               +--------------------+



+--------------------+        +--------------------+
|   GenerateReport   +---+---->   SendEmailReport  |
+--------------------+   |    +--------------------+
                         ?
                         |
                         +-topic: atmosphere-reports

```
