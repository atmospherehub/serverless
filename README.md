# Gateway for Atmosphere stations and API


## Notes

- `funproj` files are excluded for now, currently the deployment script cannot handle them 

## Pipeline
```
                                                       ++[queue] atmosphere+processed+images
                                                       |
                                                       |
                                                       |
+--------------------+        +--------------------+   +    +--------------------+
|    FacesUpload     +-------->    ProcessImage    +--+?+--->  PostProcessImage  |
+--------------------+        +--------------------+        +---------+----------+
                                                                      +
                                                                      ?+--+[topic] atmosphere+images+with+faces
                                                                      +
                         +--------------+--------------+--------------+--------------+
                         |              |              |                             |
                         |              |    +---------v----------+        +---------v----------+
                         |              |    |     StoreTable     |        |     StoreSql       |
                         |              |    +--------------------+        +---------+----------+
                         |              |                                            +
                         |    +---------v----------+                                 ?+--+[topic] atmosphere+images+in+db
                         |    |    NotifySlack     |                                 +
                         |    +--------------------+        +-------------------------------------------------+
                         |                                  |                        |                        |
                         |                        +---------v----------+   +---------v----------+   +---------v----------+
                         |                        |   StoreRectangles  |   | SendFaceForTagging |   |     StoreZoomIn    |
               +---------v----------+             +--------------------+   +--------------------+   +--------------------+
               |     NotifyMQTT     |
               +--------------------+

                                                              +-------------------+
                                                              |      FaceTag      |
+--------------------+        +--------------------+          +---------+---------+
|   GenerateReport   +--+?+--->   SendEmailReport  |                    |
+--------------------+   +    +--------------------+                    ?+----+[topic] atmosphere+face+tagging
                         |                                              |
                         |                                    +---------+---------+
                         ++[topic] atmosphere+reports         |  [subs] sql|store |
                                                              +---------+---------+
                                                                        |
                                                              +---------v---------+
                                                              |  StoreFaceTagSql  |
                                                              +---------+---------+
                                                                        |
                                                                        ?+----+[topic] atmosphere-face-recognition
                                                                        |
                                                           +------------v------------+
                                                           | [subs] face recognition |
                                                           +------------+------------+
                                                                        |
                                                                        |
                                                           +------------v------------+
                                                           |  SendFaceForRecognition |
                                                           +-------------------------+



```
