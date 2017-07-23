## Reports flow

The flow is schedule based and generates reports.

```
+--------------------+        +--------------------+
|   GenerateReport   +--+?+--->   SendEmailReport  |
+--------------------+   +    +--------------------+
                         |
                         |
                         ++[topic] atmosphere+reports

```
