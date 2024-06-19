GIT_ROOT=../../../..
SHARED_PACKAGE=Packages/Internal/com.unity.services.shared
STARBUCK2="starbuck2"

$STARBUCK2 $GIT_ROOT/$SHARED_PACKAGE/Editor/Clients             ../Editor/Core/Shared/Clients               --service Core --namespace Core.Editor.Shared
$STARBUCK2 $GIT_ROOT/$SHARED_PACKAGE/Editor/DependencyInversion ../Editor/Core/Shared/DependencyInversion   --service Core --namespace Core.Editor.Shared
$STARBUCK2 $GIT_ROOT/$SHARED_PACKAGE/Editor/EditorUtils         ../Editor/Core/Shared/EditorUtils           --service Core --namespace Core.Editor.Shared
$STARBUCK2 $GIT_ROOT/$SHARED_PACKAGE/Editor/Infrastructure      ../Editor/Core/Shared/Infrastructure        --service Core --namespace Core.Editor.Shared
$STARBUCK2 $GIT_ROOT/$SHARED_PACKAGE/Editor/UI                  ../Editor/Core/Shared/UI                    --service Core --namespace Core.Editor.Shared

$STARBUCK2 $GIT_ROOT/$SHARED_PACKAGE/Tests/Editor/Shared        ../Tests/Editor/Shared                      --service Core -d CECIL_AVAILABLE
