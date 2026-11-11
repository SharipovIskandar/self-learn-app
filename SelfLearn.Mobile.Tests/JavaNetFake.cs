// Android'da tarmoq xatolari Java.* nomlar fazosidagi istisnolar bo'lib keladi (masalan Java.Net.UnknownHostException).
// ApiClient shularni ham tarmoq xatosi deb taniydi; testda shu nomlar fazosini taqlid qilamiz.
namespace Java.Net;

internal sealed class UnknownHostException : Exception
{
}
