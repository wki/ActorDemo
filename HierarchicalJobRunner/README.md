# Hierarchical Job Runner


## Example Job Layout

  * root element (Group)
    - group "Installation"
      + DownloadJob "Download artifacts"
      + ExecuteJob "Install thing"
      + ExecuteJob "Finish whatever"
    - group "Configuration"
      + ExecuteJob "restore backups"
      + ExecuteJob "add product keys"
      + ExecuteJob "add secrets"


## Namespaces

  * Job: definitions of parts for Job Layout
    - Group: has children (group or *Job)
    - ExecuteJob: a job executing a commandline
    - DownloadJob: a job downloading things from a Url
    - Element / Node: abstract base classes for the above
  * Processing: parts for executing stuff
    - NodeExecutor: executor for everything having children (Group)
    - ElementExecutor: executor for ExecutJob/DownloadJob
    - IRunner: interface for runners/simulators
  * Running: the working part for jobs
    - DownloadRunner: do the download
    - ExecuteRunner: execute a binary
  * Simulating: simulators for working parts
