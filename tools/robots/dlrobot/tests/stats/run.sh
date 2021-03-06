PROJECT=$1
WEB_ADDR=$2
export DLROBOT_CENTRAL_SERVER_ADDRESS=localhost:8164

RESULT_FOLDER=processed_projects
INPUT_FOLDER=input_projects
WORKER_DIR=workdir

rm -rf $INPUT_FOLDER $RESULT_FOLDER *.log *.html $WORKER_DIR
mkdir $INPUT_FOLDER
mv $PROJECT $INPUT_FOLDER

python3 ../../scripts/cloud/dlrobot_central.py --input-folder input_projects --result-folder  ${RESULT_FOLDER} --disable-smart-parser-cache &
CENTRAL_PID=$!
sleep 1

python3 ../../scripts/cloud/dlrobot_worker.py run_once --working-folder ${WORKER_DIR}

if [ $? != 0 ]; then
  echo "dlrobot_worker.py failed"
  kill $CENTRAL_PID
fi

kill $CENTRAL_PID

python3 ../../scripts/cloud/dlrobot_stats.py --central-stats-file $RESULT_FOLDER/dlrobot_remote_calls.dat
if [ $? != 0 ]; then
  echo "dlrobot_stats.py failed"
  exit 1
fi
