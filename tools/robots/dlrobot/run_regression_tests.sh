if [ "$#" -ne 1 ]; then
    echo "Illegal number of parameters"
fi

FOLDER=$1
rm $FOLDER/*.txt.visited_pages $FOLDER/*.txt.result_summary $FOLDER/*.txt.click_paths $FOLDER/*.txt.log

ls $FOLDER/*.txt | xargs --verbose -n 1 -P 4 python3 dlrobot.py --project

if [ $? -eq 0 ]; then
    echo  "==== TESTS PASSED ====="!
else
    echo  "==== TESTS FAILED ====="!
fi
